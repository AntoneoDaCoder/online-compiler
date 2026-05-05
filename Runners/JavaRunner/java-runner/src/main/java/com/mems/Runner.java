package com.mems;

import java.io.BufferedReader;
import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.InputStreamReader;
import java.net.InetSocketAddress;
import java.net.URI;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.util.ArrayList;
import java.util.Arrays;
import java.util.List;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.TimeUnit;
import java.util.stream.Collectors;

import javax.tools.JavaCompiler;
import javax.tools.ToolProvider;
import com.sun.net.httpserver.HttpExchange;
import com.sun.net.httpserver.HttpHandler;
import com.sun.net.httpserver.HttpServer;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.github.javaparser.StaticJavaParser;
import com.github.javaparser.ast.CompilationUnit;
import com.github.javaparser.ast.expr.MethodCallExpr;
import com.github.javaparser.ast.expr.ObjectCreationExpr;
import java.util.UUID;
import com.mems.helpers.*;
import com.mems.Shared.DTOs.CodeResponseDto;
import com.mems.Shared.DTOs.ExecutionResultDto;
import com.mems.Shared.DTOs.ProblemSolutionDto;
import com.mems.Shared.Enums.ExecutionStatus;
import com.mems.Shared.Enums.RequestStatus;
import java.time.OffsetDateTime;
import com.fasterxml.jackson.annotation.JsonIgnoreProperties;

public class Runner 
{
    private static final String REPORT_BEGIN_MARKER = "__TEST_REPORT_BEGIN__";
    private static final String REPORT_END_MARKER = "__TEST_REPORT_END__";

    @JsonIgnoreProperties(ignoreUnknown = true)
    public static class TestRunReportDto {
        public int totalTests;
        public int passedTests;
        public List<FailedTestDto> failedTests = new ArrayList<>();
    }

    private static TestRunReportDto tryParseReport(String output) {
        if (output == null || output.isBlank()) return null;

        int begin = output.indexOf(REPORT_BEGIN_MARKER);
        if (begin < 0) return null;
        begin += REPORT_BEGIN_MARKER.length();

        int end = output.indexOf(REPORT_END_MARKER, begin);
        if (end < 0 || end <= begin) return null;

        String json = output.substring(begin, end).trim();
        if (json.isEmpty()) return null;

        try {
            return objectMapper.readValue(json, TestRunReportDto.class);
        } catch (Exception e) {
            return null;
        }
    }

    private static String buildConsoleOutput(TestRunReportDto report, String rawOutput) {
        if (report != null) {
            if (report.failedTests == null || report.failedTests.isEmpty()) return "";
            return report.failedTests.stream()
                    .map(f -> (f.name == null ? "unknown" : f.name) + ": " + (f.reason == null ? "" : f.reason))
                    .collect(Collectors.joining("\n"));
        }
        return rawOutput == null ? "" : rawOutput.trim();
    }

    @JsonIgnoreProperties(ignoreUnknown = true)
    public static class FailedTestDto {
        public String name;
        public String reason;
    }

    private static final String TMP_DIR = System.getProperty("java.io.tmpdir");
    private static final String LANG_CODE = "java";
    private static final String TMP_CLASS_NAME = "GeneratedTests";
    private static final String TMP_JAVA_FILE = TMP_DIR + "/GeneratedTests.java";
    private static final String TMP_CLASS_FILE = TMP_DIR + "/GeneratedTests.class";
    private static final String API_CALLBACK_URL = "http://api-server.default.svc.cluster.local:8080/api/jobs/complete";
    private static final int MAX_PROCESS_LIFETIME_MS = 25000;
    private static final int RUNNER_PORT = 5000;

    
    private static final HttpClient httpClient = HttpClient.newHttpClient();
    private static final ObjectMapper objectMapper = JsonUtils.getObjectMapper();

    public static void main(String[] args) throws Exception {
        Runtime.getRuntime().addShutdownHook(new Thread(Runner::releaseResources));

        if (args.length > 0 && args[0].equals("--server")) {
            startServer();
        } else if (args.length > 0 && args[0].equals("--once")) {
            runOnce();
        } else if (args.length > 0 && args[0].equals("--test") && args.length > 1) {
            // Local test mode: --test /path/to/request.json
            runLocalTestFile(args[1]);
        } else {
            System.out.println("Usage: java -jar runner.jar [--server | --once | --test <request.json>]");
        }
    }

    /**
     * Local helper: read ProblemSolutionDto JSON from file, parse manifest, generate UserProgram.java and write it to cwd.
     * - requestJsonPath : path to JSON file that matches C# ProblemSolutionDto
     */
    private static void runLocalTestFile(String requestJsonPath) {
        try {
            System.out.println("[JavaRunner] Local test mode. Reading: " + requestJsonPath);
            String body = java.nio.file.Files.readString(java.nio.file.Paths.get(requestJsonPath), java.nio.charset.StandardCharsets.UTF_8);

            // ObjectMapper уже должен быть настроен (см. шаг 2)
            ObjectMapper mapper = JsonUtils.getObjectMapper();

            ProblemSolutionDto request = null;

            // Попробуем распарсить как ProblemSolutionDto
            try {
                request = mapper.readValue(body, ProblemSolutionDto.class);
            } catch (Exception ex) {
                // ignore for now; попробуем интерпретировать как манифест прямо
            }

            // Если распарсили, но TestManifestJson пуст — возможно, файл был манифестом или частично заполнен
            if (request == null || request.TestManifestJson == null || request.TestManifestJson.trim().isEmpty()) {
                // попытаемся понять: если body содержит поле "entrypoint" или "signature" — считаем, что это сам манифест
                boolean looksLikeManifest = body.contains("\"entrypoint\"") || body.contains("\"signature\"") || body.contains("\"sampleTests\"");
                if (looksLikeManifest) {
                    // обернём манифест в ProblemSolutionDto автоматически
                    request = new ProblemSolutionDto();
                    request.RequestId = java.util.UUID.randomUUID().toString();
                    request.VersionId = java.util.UUID.randomUUID().toString();
                    request.UserId = java.util.UUID.randomUUID().toString();
                    request.LanguageCode = "java";
                    request.UserSolution = ""; // will be replaced below with adapted Java code
                    request.SentAt = java.time.OffsetDateTime.now();
                    // store manifest JSON as string (escape not required - it's already JSON text)
                    request.TestManifestJson = body;
                    System.out.println("[JavaRunner] Input looks like a manifest; wrapped into ProblemSolutionDto.");
                } else {
                    // не удалось распарсить
                    throw new IllegalArgumentException("Input file is neither ProblemSolutionDto nor Manifest JSON");
                }
            }

            // далее процесс как раньше — получить manifest и сгенерировать user program
            com.mems.manifest.ManifestDto manifest = com.mems.helpers.ManifestParser.parse(request.TestManifestJson,LANG_CODE);

            // --- ADAPTATION: convert provided C# Solution to Java implementation ---
            // For convenience we ignore request.UserSolution (C#) and inject a Java equivalent of your C# method.
            // If you want to use request.UserSolution as Java code, replace 'userJavaCode' with request.UserSolution.
            String userJavaCode = """            
            public static int[] Solution(String mode, int[] arr) {
                if (arr == null) return new int[0];
                mode = (mode == null) ? "" : mode;

                switch (mode) {
                    case "identity":
                        return java.util.Arrays.copyOf(arr, arr.length);

                    case "sort":
                        int[] copy = java.util.Arrays.copyOf(arr, arr.length);
                        java.util.Arrays.sort(copy);
                        return copy;

                    case "sleep":
                        // блокирующий sleep — чтобы тест с малым таймаутом провалился
                        try {
                            Thread.sleep(5000);
                        } catch (InterruptedException ie) {
                            // ignore
                        }
                        return java.util.Arrays.copyOf(arr, arr.length);

                    case "sum_as_array":
                        int s = 0;
                        for (int x : arr) s += x;
                        return new int[] { s };

                    default:
                        return java.util.Arrays.copyOf(arr, arr.length);
                }
            }
            """;

            // generate full source using JavaWrapper
            ITestWrapper wrapper = new com.mems.helpers.JavaWrapper();
            // languageCode: "java"
            String fullSource = wrapper.generateSource(manifest, "java", userJavaCode, "SolutionContainer", 2000L);

            // write to file in current working directory
            java.nio.file.Path out = java.nio.file.Paths.get("GeneratedTests.java");
            java.nio.file.Files.writeString(out, fullSource, java.nio.charset.StandardCharsets.UTF_8);
            System.out.println("[JavaRunner] Generated source written to: " + out.toAbsolutePath().toString());
            System.out.println("[JavaRunner] To compile & run locally (example):");
            System.out.println("  1) ensure junit jar(s) are available, e.g.: junit-4.13.2.jar and hamcrest-core-1.3.jar");
            System.out.println("  2) compile: javac -cp .:junit-4.13.2.jar:hamcrest-core-1.3.jar UserProgram.java");
            System.out.println("     (on Windows use ';' as classpath separator)");
            System.out.println("  3) run: java -cp .:junit-4.13.2.jar:hamcrest-core-1.3.jar UserProgram");
            System.out.println("     Program will print PassedTests:<n> to stdout and failure details to stderr.");
        } catch (Exception e) {
            System.err.println("[JavaRunner] runLocalTestFile failed: " + e);
            e.printStackTrace();
        }
    }


    private static void startServer() throws IOException {
        HttpServer server = HttpServer.create(new InetSocketAddress(RUNNER_PORT), 0);
        server.createContext("/run", new RequestHandler());
        server.start();

        System.out.println("[JavaRunner] Java runner started on port " + RUNNER_PORT);
    }

    private static void runOnce() {
        try {
            String requestJson = new String(System.in.readAllBytes(), StandardCharsets.UTF_8);
            ProblemSolutionDto request = objectMapper.readValue(requestJson, ProblemSolutionDto.class);

            CodeResponseDto response = executeUserCodeOnce(request);

            String responseJson = objectMapper.writeValueAsString(response);
            // IMPORTANT: print JSON to STDOUT so C# wrapper can read it
            System.out.println(responseJson);

        } catch (Exception e) {
            System.err.println("[JavaRunner] CLI mode failed: " + e);
            e.printStackTrace();
        }
    }


    private static CodeResponseDto executeUserCodeOnce(ProblemSolutionDto request) {
        CodeResponseDto response = new CodeResponseDto();
        // map identifiers (GUID strings -> UUID)
        try {
            response.RequestId = request.RequestId != null ? UUID.fromString(request.RequestId) : UUID.randomUUID();
        } catch (Exception ex) {
            response.RequestId = UUID.randomUUID();
        }
        response.Language = request.LanguageCode;
        response.UserSolution = request.UserSolution;
        try {
            response.VersionId = request.VersionId != null ? UUID.fromString(request.VersionId) : null;
        } catch (Exception ex) { response.VersionId = null; }
        try {
            response.UserId = request.UserId != null ? UUID.fromString(request.UserId) : null;
        } catch (Exception ex) { response.UserId = null; }

        response.Result = new ExecutionResultDto();
        response.Result.RequestSentAt = request.SentAt != null ? request.SentAt : OffsetDateTime.now();

        try {
            // parse manifest from TestManifestJson
            com.mems.manifest.ManifestDto manifest = com.mems.helpers.ManifestParser.parse(request.TestManifestJson,LANG_CODE);

            // set TotalTests from manifest (sample + advanced)
            int total = 0;
            if (manifest.sampleTests != null) total += manifest.sampleTests.size();
            if (manifest.advancedTests != null) total += manifest.advancedTests.size();
            response.Result.TotalTests = total;

            String fullCode = wrapUserCode(request, manifest);
            Files.writeString(Paths.get(TMP_JAVA_FILE), fullCode);

            List<String> violations = checkForbiddenAPIs(fullCode);

            if (!violations.isEmpty()) {
                response.Status = RequestStatus.FAILED;
                response.Result.Status = ExecutionStatus.CANCELLED;
                response.Result.ExitCode = 2;
                response.Result.ConsoleOutput = String.join("\n", violations);
                response.Result.ResponseSentAt = OffsetDateTime.now();
                return response;
            }

            ByteArrayOutputStream errorOutput = new ByteArrayOutputStream();
            boolean compiled = compileJavaFile(TMP_JAVA_FILE, errorOutput);

            if (!compiled) {
                response.Status = RequestStatus.FAILED;
                response.Result.Status = ExecutionStatus.COMPILE_ERROR;
                response.Result.ExitCode = 1;

                String fullError = errorOutput.toString(StandardCharsets.UTF_8);
                int index = fullError.indexOf("error:");
                if (index != -1) fullError = fullError.substring(index);

                response.Result.ConsoleOutput = fullError;
                response.Result.ResponseSentAt = OffsetDateTime.now();
                return response;
            }

            String separator = System.getProperty("path.separator");
            String classpath = TMP_DIR + separator + getJunitClasspath();

            ProcessBuilder pb = new ProcessBuilder("java", "-cp", classpath, TMP_CLASS_NAME);
            pb.redirectErrorStream(true);

            Process process = pb.start();
            StringBuilder output = new StringBuilder();

            Thread outputReader = new Thread(() -> {
                try (BufferedReader reader = new BufferedReader(new InputStreamReader(process.getInputStream(), StandardCharsets.UTF_8))) {
                    String line;
                    while ((line = reader.readLine()) != null) {
                        output.append(line).append("\n");
                    }
                } catch (IOException e) {
                    e.printStackTrace();
                }
            });
            outputReader.start();

            boolean completed = process.waitFor(MAX_PROCESS_LIFETIME_MS, TimeUnit.MILLISECONDS);
            outputReader.join();

            if (!completed) {
                process.destroyForcibly();
                response.Status = RequestStatus.FAILED;
                response.Result.Status = ExecutionStatus.TIMED_OUT;
                response.Result.ExitCode = 124;
                response.Result.ConsoleOutput = "Execution timed out";
                response.Result.ResponseSentAt = OffsetDateTime.now();
            } else {
                response.Result.ExitCode = process.exitValue();
                String fullOut = output.toString();
                TestRunReportDto report = tryParseReport(fullOut);

                if (report != null) {
                    response.Result.TotalTests = report.totalTests;
                    response.Result.PassedTests = report.passedTests;
                    response.Result.ConsoleOutput = buildConsoleOutput(report, fullOut);

                    if (report.failedTests == null || report.failedTests.isEmpty()) {
                        response.Status = RequestStatus.SUCCEEDED;
                        response.Result.Status = ExecutionStatus.SUCCEEDED;
                        response.Result.ExitCode = process.exitValue();
                    } else {
                        response.Status = RequestStatus.FAILED;
                        response.Result.Status = ExecutionStatus.FAILED_TO_EXECUTE;
                        response.Result.ExitCode = process.exitValue() != 0 ? process.exitValue() : 1;
                    }
                } else {
                    response.Status = RequestStatus.FAILED;
                    response.Result.ExitCode = process.exitValue();

                    if (process.exitValue() == 0) {
                        response.Result.Status = ExecutionStatus.RUNTIME_ERROR;
                    } else {
                        response.Result.Status = ExecutionStatus.RUNTIME_ERROR;
                    }

                    response.Result.ConsoleOutput = fullOut.trim();
                }
            }
        } catch (Exception e) {
            response.Status = RequestStatus.FAILED;
            response.Result.Status = ExecutionStatus.RUNTIME_ERROR;
            response.Result.ConsoleOutput = e.toString();
            response.Result.ResponseSentAt = OffsetDateTime.now();
            System.err.println("[JavaRunner] Exception occurred: " + e);
        } finally {
            cleanupTempFiles();
        }

        return response;
    }


    public static List<String> checkForbiddenAPIs(String sourceCode) {
        CompilationUnit cu = StaticJavaParser.parse(sourceCode);
        List<String> violations = new ArrayList<>();

        // ProcessBuilder
        cu.findAll(ObjectCreationExpr.class).forEach(expr -> {
            String typeName = expr.getType().getNameAsString();
            if ("ProcessBuilder".equals(typeName)) {
                violations.add("Usage of ProcessBuilder is forbidden.");
            }
        });

        cu.findAll(MethodCallExpr.class).forEach(method -> {
            String methodName = method.getNameAsString();

            // System.load
            if ("load".equals(methodName) || "loadLibrary".equals(methodName)) {
                method.getScope().ifPresent(scope -> {
                    if (scope.toString().equals("System")) {
                        violations.add("Usage of System." + methodName + "() is forbidden.");
                    }
                });
            }
        });

        return violations;
    }

    static class RequestHandler implements HttpHandler {
        @Override
        public void handle(HttpExchange exchange) throws IOException {
            try {
                String requestBody = new String(exchange.getRequestBody().readAllBytes(), StandardCharsets.UTF_8);
                ProblemSolutionDto request = objectMapper.readValue(requestBody, ProblemSolutionDto.class);

                System.out.println("[JavaRunner] Received request [Id:" + request.RequestId + "]");

                CompletableFuture.runAsync(() -> executeUserCode(request));

                exchange.sendResponseHeaders(200, -1);
            } catch (Exception e) {
                System.err.println("[JavaRunner] Error processing request: " + e);
                exchange.sendResponseHeaders(500, -1);
            } finally {
                exchange.close();
            }
        }
    }


    private static void executeUserCode(ProblemSolutionDto request) {
        CodeResponseDto response = new CodeResponseDto();
        try {
            response.RequestId = request.RequestId != null ? UUID.fromString(request.RequestId) : UUID.randomUUID();
        } catch (Exception ex) { response.RequestId = UUID.randomUUID(); }

        response.Language = request.LanguageCode;
        response.UserSolution = request.UserSolution;
        try { response.VersionId = request.VersionId != null ? UUID.fromString(request.VersionId) : null; } catch (Exception ex) { response.VersionId = null; }
        try { response.UserId = request.UserId != null ? UUID.fromString(request.UserId) : null; } catch (Exception ex) { response.UserId = null; }

        response.Result = new ExecutionResultDto();
        response.Result.RequestSentAt = request.SentAt != null ? request.SentAt : OffsetDateTime.now();

        try {
            com.mems.manifest.ManifestDto manifest = com.mems.helpers.ManifestParser.parse(request.TestManifestJson,LANG_CODE);

            int total = 0;
            if (manifest.sampleTests != null) total += manifest.sampleTests.size();
            if (manifest.advancedTests != null) total += manifest.advancedTests.size();
            response.Result.TotalTests = total;

            String fullCode = wrapUserCode(request, manifest);
            Files.writeString(Paths.get(TMP_JAVA_FILE), fullCode);

            List<String> violations = checkForbiddenAPIs(fullCode);

            if (!violations.isEmpty()) {
                response.Status = RequestStatus.FAILED;
                response.Result.Status = ExecutionStatus.CANCELLED;
                response.Result.ExitCode = 2;
                response.Result.ConsoleOutput = String.join("\n", violations);
                response.Result.ResponseSentAt = OffsetDateTime.now();
                notifyJobManager(response);
                return;
            }

            ByteArrayOutputStream errorOutput = new ByteArrayOutputStream();
            boolean compiled = compileJavaFile(TMP_JAVA_FILE, errorOutput);

            if (!compiled) {
                response.Status = RequestStatus.FAILED;
                response.Result.Status = ExecutionStatus.COMPILE_ERROR;
                response.Result.ExitCode = 1;

                String fullError = errorOutput.toString(StandardCharsets.UTF_8);
                int index = fullError.indexOf("error:");
                if (index != -1) fullError = fullError.substring(index);

                response.Result.ConsoleOutput = fullError;
                response.Result.ResponseSentAt = OffsetDateTime.now();
                notifyJobManager(response);
                return;
            }

            String separator = System.getProperty("path.separator");
            String classpath = TMP_DIR + separator + getJunitClasspath();

            ProcessBuilder pb = new ProcessBuilder("java", "-cp", classpath, TMP_CLASS_NAME);
            pb.redirectErrorStream(true);

            Process process = pb.start();
            StringBuilder output = new StringBuilder();
            Thread outputReader = new Thread(() -> {
                try (BufferedReader reader = new BufferedReader(new InputStreamReader(process.getInputStream(), StandardCharsets.UTF_8))) {
                    String line;
                    while ((line = reader.readLine()) != null) {
                        output.append(line).append("\n");
                    }
                } catch (IOException e) {
                    e.printStackTrace();
                }
            });
            outputReader.start();

            boolean completed = process.waitFor(MAX_PROCESS_LIFETIME_MS, TimeUnit.MILLISECONDS);
            outputReader.join();

            if (!completed) {
                process.destroyForcibly();
                response.Status = RequestStatus.FAILED;
                response.Result.Status = ExecutionStatus.TIMED_OUT;
                response.Result.ExitCode = 124;
                response.Result.ConsoleOutput = "Execution timed out";
                response.Result.ResponseSentAt = OffsetDateTime.now();
            } else {
                response.Result.ExitCode = process.exitValue();
                String fullOut = output.toString();
                TestRunReportDto report = tryParseReport(fullOut);

                if (report != null) {
                    response.Result.TotalTests = report.totalTests;
                    response.Result.PassedTests = report.passedTests;
                    response.Result.ConsoleOutput = buildConsoleOutput(report, fullOut);

                    if (report.failedTests == null || report.failedTests.isEmpty()) {
                        response.Status = RequestStatus.SUCCEEDED;
                        response.Result.Status = ExecutionStatus.SUCCEEDED;
                        response.Result.ExitCode = process.exitValue();
                    } else {
                        response.Status = RequestStatus.FAILED;
                        response.Result.Status = ExecutionStatus.FAILED_TO_EXECUTE;
                        response.Result.ExitCode = process.exitValue() != 0 ? process.exitValue() : 1;
                    }
                } else {
                    response.Status = RequestStatus.FAILED;
                    response.Result.ExitCode = process.exitValue();

                    if (process.exitValue() == 0) {
                        response.Result.Status = ExecutionStatus.RUNTIME_ERROR;
                    } else {
                        response.Result.Status = ExecutionStatus.RUNTIME_ERROR;
                    }

                    response.Result.ConsoleOutput = fullOut.trim();
                }
            }
        } catch (Exception e) {
            response.Status = RequestStatus.FAILED;
            response.Result.Status = ExecutionStatus.RUNTIME_ERROR;
            response.Result.ConsoleOutput = e.toString();
            response.Result.ResponseSentAt = OffsetDateTime.now();
            System.out.println("[JavaRunner] Exception occurred:" + e);
        } finally {
            cleanupTempFiles();
            notifyJobManager(response);
        }
    }


    private static boolean compileJavaFile(String javaFilePath, ByteArrayOutputStream errorOut) {
        try {
            JavaCompiler compiler = ToolProvider.getSystemJavaCompiler();

            if (compiler == null) {
                System.err.println("[JavaRunner] JavaCompiler not available.");

                return false;
            }

            int compileResult = compiler.run(
                null,
                null,
                errorOut,
                "-d", TMP_DIR,
                javaFilePath
            );

            if (compileResult != 0) {
                System.err.println("[JavaRunner] Compilation failed with exit code: " + compileResult);
                System.err.println("[JavaRunner] Compiler output:\n" + errorOut.toString(StandardCharsets.UTF_8));

                return false;
            }

            return true;
        } catch (Exception e) {
            System.err.println("Exception during compilation: " + e.getMessage());
            e.printStackTrace();

            return false;
        }
    }

    private static String wrapUserCode(ProblemSolutionDto request, com.mems.manifest.ManifestDto manifest) throws Exception {
        String userCode = request.UserSolution == null ? "" : request.UserSolution;
        long defaultTimeoutMs = 2000L;

        ITestWrapper wrapper = new com.mems.helpers.JavaWrapper();
        String fullSource = wrapper.generateSource(manifest, request.LanguageCode == null ? "java" :
                request.LanguageCode, userCode, "SolutionContainer", defaultTimeoutMs);

        // keep previous behavior: strip package declarations
        return fullSource.replaceFirst("(?m)^\\s*package\\s+[^;]+;\\s*", "");
    }
    
    private static void notifyJobManager(CodeResponseDto response) {
        response.Result.ResponseSentAt = OffsetDateTime.now();
        
        try {
            HttpRequest request = HttpRequest.newBuilder()
                .uri(URI.create(API_CALLBACK_URL))
                .header("Content-Type", "application/json")
                .POST(HttpRequest.BodyPublishers.ofString(objectMapper.writeValueAsString(response)))
                .build();
                
            HttpResponse<String> httpResponse = httpClient.send(request, HttpResponse.BodyHandlers.ofString());

            System.out.println("[JavaRunner] Sent response [Id:" + response.RequestId + "]");
            System.out.println("[JavaRunner] HTTP Status code: " + httpResponse.statusCode());
        } catch (Exception e) {
            System.err.println("[JavaRunner] Failed to send response [Id:" + response.RequestId + "]: " + e);
        }
    }
    
    private static String getJunitClasspath() {
        return System.getProperty("java.class.path");
    }
    
    private static void cleanupTempFiles() {
        try {
            Files.deleteIfExists(Paths.get(TMP_JAVA_FILE));
            Files.deleteIfExists(Paths.get(TMP_CLASS_FILE));
        } catch (IOException e) {
            System.err.println("Error cleaning temp files: " + e);
        }
    }
    
    private static void releaseResources() {
        cleanupTempFiles();
    }
}