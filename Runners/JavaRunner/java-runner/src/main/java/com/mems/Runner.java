package com.mems;

import java.io.BufferedReader;
import java.io.BufferedWriter;
import java.io.ByteArrayOutputStream;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.OutputStreamWriter;
import java.net.InetSocketAddress;
import java.net.URI;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;
import java.nio.charset.StandardCharsets;
import java.nio.file.Files;
import java.nio.file.Paths;
import java.time.OffsetDateTime;
import java.util.ArrayList;
import java.util.List;
import java.util.UUID;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.TimeUnit;
import java.util.stream.Collectors;

import javax.tools.JavaCompiler;
import javax.tools.ToolProvider;

import com.fasterxml.jackson.annotation.JsonIgnoreProperties;
import com.fasterxml.jackson.annotation.JsonProperty;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.github.javaparser.StaticJavaParser;
import com.github.javaparser.ast.CompilationUnit;
import com.github.javaparser.ast.expr.MethodCallExpr;
import com.github.javaparser.ast.expr.ObjectCreationExpr;
import com.mems.Shared.DTOs.CodeResponseDto;
import com.mems.Shared.DTOs.ExecutionResultDto;
import com.mems.Shared.DTOs.ProblemSolutionDto;
import com.mems.Shared.Enums.ExecutionStatus;
import com.mems.Shared.Enums.RequestStatus;
import com.mems.helpers.ITestWrapper;
import com.mems.helpers.JavaWrapper;
import com.mems.helpers.JsonUtils;
import com.mems.helpers.ManifestParser;
import com.mems.manifest.ManifestDto;
import com.sun.net.httpserver.HttpExchange;
import com.sun.net.httpserver.HttpHandler;
import com.sun.net.httpserver.HttpServer;

public class Runner {
    private static final String REPORT_BEGIN_MARKER = "__TEST_REPORT_BEGIN__";
    private static final String REPORT_END_MARKER = "__TEST_REPORT_END__";

    private long globalTimeoutMs = 25_000L;

    @JsonIgnoreProperties(ignoreUnknown = true)
    private static class RunRequestDto {
        @JsonProperty("executorFileName")
        public String executorFileName = "";

        @JsonProperty("commandLineArguments")
        public List<String> commandLineArguments = new ArrayList<>();

        @JsonProperty("executableFileName")
        public String executableFileName = "";

        @JsonProperty("maxProcessLifetime")
        public int maxProcessLifetime = 0;
    }

    @JsonIgnoreProperties(ignoreUnknown = true)
    private static class RunResultDto {
        @JsonProperty("wallTimeMs")
        public Long wallTimeMs = 0L;

        @JsonProperty("cpuTimeUs")
        public Long cpuTimeUs = 0L;

        @JsonProperty("peakMemoryBytes")
        public Long peakMemoryBytes = 0L;

        @JsonProperty("testReport")
        public TestRunReportDto testReport = null;

        @JsonProperty("status")
        public ExecutionStatus status = ExecutionStatus.NO_STATUS;

        @JsonProperty("state")
        public String state = "";

        @JsonProperty("stdOut")
        public String stdOut = null;

        @JsonProperty("stdErr")
        public String stdErr = null;

        @JsonProperty("exitCode")
        public int exitCode = 0;
    }

    @JsonIgnoreProperties(ignoreUnknown = true)
    private static class TestRunReportDto {
        @JsonProperty("totalTests")
        public int totalTests = 0;

        @JsonProperty("passedTests")
        public int passedTests = 0;

        @JsonProperty("failedTests")
        public List<FailedTestDto> failedTests = new ArrayList<>();
    }

    @JsonIgnoreProperties(ignoreUnknown = true)
    private static class FailedTestDto {
        @JsonProperty("name")
        public String name = null;

        @JsonProperty("reason")
        public String reason = null;
    }

    private static class CompileResult {
        final boolean success;
        final String output;

        CompileResult(boolean success, String output) {
            this.success = success;
            this.output = output;
        }
    }

    private final ProcessBuilder supervisorProcessBuilder;
    private static final ObjectMapper objectMapper = JsonUtils.getObjectMapper();
    private static final HttpClient httpClient = HttpClient.newHttpClient();

    private static final String TMP_DIR = System.getProperty("java.io.tmpdir");
    private static final String LANG_CODE = "java";
    private static final String TMP_CLASS_NAME = "GeneratedTests";
    private static final String TMP_JAVA_FILE = TMP_DIR + "/GeneratedTests.java";
    private static final String TMP_CLASS_FILE = TMP_DIR + "/GeneratedTests.class";
    private static final String API_CALLBACK_URL = "http://api-server.default.svc.cluster.local:8080/api/jobs/complete";
    private static final int MAX_PROCESS_LIFETIME_MS = 25000;
    private static final int RUNNER_PORT = 5000;

    public Runner(ProcessBuilder supervisorProcessBuilder) {
        this.supervisorProcessBuilder = supervisorProcessBuilder;
    }

    public static void main(String[] args) throws Exception {
        Runtime.getRuntime().addShutdownHook(new Thread(Runner::releaseResources));

        Runner runner = new Runner(new ProcessBuilder("./RunnerSupervisor"));

        if (args.length > 0 && args[0].equals("--server")) {
            runner.startServer();
        } else if (args.length > 0 && args[0].equals("--once")) {
            runner.runOnce();
        } else if (args.length > 0 && args[0].equals("--test") && args.length > 1) {
            runner.runLocalTestFile(args[1]);
        } else {
            System.out.println("Usage: java -jar runner.jar [--server | --once | --test <request.json>]");
        }
    }

    private void runLocalTestFile(String requestJsonPath) {
        try {
            String body = Files.readString(Paths.get(requestJsonPath), StandardCharsets.UTF_8);
            ProblemSolutionDto request = objectMapper.readValue(body, ProblemSolutionDto.class);
            CodeResponseDto response = executeUserCodeOnce(request);
            System.out.println(objectMapper.writeValueAsString(response));
        } catch (Exception e) {
            System.err.println("[JavaRunner] runLocalTestFile failed: " + e);
            e.printStackTrace();
        }
    }

    private void startServer() throws IOException {
        HttpServer server = HttpServer.create(new InetSocketAddress(RUNNER_PORT), 0);
        server.createContext("/run", new RequestHandler());
        server.start();

        System.out.println("[JavaRunner] Java runner started on port " + RUNNER_PORT);
    }

    private void runOnce() {
        try {
            String requestJson = new String(System.in.readAllBytes(), StandardCharsets.UTF_8);
            ProblemSolutionDto request = objectMapper.readValue(requestJson, ProblemSolutionDto.class);
            CodeResponseDto response = executeUserCodeOnce(request);

            String responseJson = objectMapper.writeValueAsString(response);
            System.out.println(responseJson);
        } catch (Exception e) {
            System.err.println("[JavaRunner] CLI mode failed: " + e);
            e.printStackTrace();
        }
    }

    public CodeResponseDto executeUserCodeOnce(ProblemSolutionDto request) {
        OffsetDateTime now = OffsetDateTime.now();

        CodeResponseDto response = new CodeResponseDto();
        response.RequestId = parseUuidOrRandom(request.RequestId);
        response.UserId = parseUuidOrRandom(request.UserId);
        response.VersionId = parseUuidOrNull(request.VersionId);
        response.Language = LANG_CODE;
        response.UserSolution = request.UserSolution;
        response.Result = new ExecutionResultDto();
        response.Result.RequestSentAt = request.SentAt != null ? request.SentAt : now;
        response.Result.ResponseSentAt = now;

        try {
            ManifestDto manifest = ManifestParser.parse(request.TestManifestJson, LANG_CODE);

            int total = 0;
            if (manifest.sampleTests != null) total += manifest.sampleTests.size();
            if (manifest.advancedTests != null) total += manifest.advancedTests.size();
            response.Result.TotalTests = total;

            String fullCode = wrapUserCode(request, manifest);
            Files.writeString(Paths.get(TMP_JAVA_FILE), fullCode, StandardCharsets.UTF_8);

            List<String> violations = checkForbiddenAPIs(fullCode);
            if (!violations.isEmpty()) {
                response.Status = RequestStatus.FAILED;
                response.Result.Status = ExecutionStatus.CANCELLED;
                response.Result.ExitCode = 2;
                response.Result.ConsoleOutput = String.join("\n", violations);
                response.Result.ResponseSentAt = OffsetDateTime.now();
                cleanupTempFiles();
                return response;
            }

            ByteArrayOutputStream errorOutput = new ByteArrayOutputStream();
            CompileResult compiled = compileJavaFile(TMP_JAVA_FILE, errorOutput);
            if (!compiled.success) {
                response.Status = RequestStatus.FAILED;
                response.Result.Status = ExecutionStatus.COMPILE_ERROR;
                response.Result.ExitCode = 1;
                response.Result.ConsoleOutput = compiled.output != null ? compiled.output : errorOutput.toString(StandardCharsets.UTF_8);
                response.Result.ResponseSentAt = OffsetDateTime.now();
                cleanupTempFiles();
                return response;
            }

            RunResultDto runResult = runTestsViaSupervisor();

            TestRunReportDto report = runResult.testReport;
            int passed = report != null ? report.passedTests : 0;
            int actualTotal = report != null ? report.totalTests : total;
            String consoleSummary = buildConsoleOutput(report, runResult.stdOut, runResult.stdErr);

            response.Result.PassedTests = passed;
            response.Result.TotalTests = actualTotal;
            response.Result.Status = runResult.status;
            response.Result.ExitCode = runResult.exitCode;
            response.Result.ResponseSentAt = OffsetDateTime.now();
            response.Result.WallTimeMs = runResult.wallTimeMs;
            response.Result.CpuTimeUs = runResult.cpuTimeUs;
            response.Result.PeakMemoryBytes = runResult.peakMemoryBytes;
            response.Result.ConsoleOutput =
                    "State: " + runResult.state + "\n" + consoleSummary;

            response.Status = isRequestSucceeded(runResult)
                    ? RequestStatus.SUCCEEDED
                    : RequestStatus.FAILED;

            cleanupTempFiles();
            return response;
        } catch (Exception e) {
            response.Status = RequestStatus.FAILED;
            response.Result.Status = ExecutionStatus.RUNTIME_ERROR;
            response.Result.ExitCode = 1;
            response.Result.ConsoleOutput = e.toString();
            response.Result.ResponseSentAt = OffsetDateTime.now();

            cleanupTempFiles();
            return response;
        }
    }

    private void executeUserCode(ProblemSolutionDto request) {
        CodeResponseDto response = executeUserCodeOnce(request);
        notifyJobManager(response);
    }

    private RunResultDto runTestsViaSupervisor() throws Exception {
        String separator = System.getProperty("path.separator");
        String classpath = TMP_DIR + separator + getJunitClasspath();
        String javaBin = System.getProperty("java.home") + java.io.File.separator + "bin" + java.io.File.separator + "java";

        RunRequestDto request = new RunRequestDto();
        request.executorFileName = javaBin;
        request.commandLineArguments = List.of("-cp", classpath);
        request.executableFileName = TMP_CLASS_NAME;
        request.maxProcessLifetime = MAX_PROCESS_LIFETIME_MS;

        String serializedRequest = objectMapper.writeValueAsString(request);

        Process process = supervisorProcessBuilder.start();

        CompletableFuture<String> stdoutFuture = CompletableFuture.supplyAsync(() -> readAll(process.getInputStream()));
        CompletableFuture<String> stderrFuture = CompletableFuture.supplyAsync(() -> readAll(process.getErrorStream()));

        try (BufferedWriter writer = new BufferedWriter(
                new OutputStreamWriter(process.getOutputStream(), StandardCharsets.UTF_8))) {
            writer.write(serializedRequest);
            writer.flush();
        }

        long startedAt = System.nanoTime();
        boolean finished = process.waitFor(globalTimeoutMs, TimeUnit.MILLISECONDS);

        if (!finished) {
            try {
                process.destroyForcibly();
            } catch (Exception ignored) {
            }

            return new RunResultDto() {{
                status = ExecutionStatus.TIMED_OUT;
                state = "Supervisor timed out.";
                exitCode = 124;
                wallTimeMs = TimeUnit.NANOSECONDS.toMillis(System.nanoTime() - startedAt);
                stdOut = safeGet(stdoutFuture);
                stdErr = safeGet(stderrFuture);
            }};
        }

        String stdout = safeGet(stdoutFuture);
        String stderr = safeGet(stderrFuture);

        RunResultDto parsed = tryParseSupervisorResult(stdout);
        if (parsed != null) {
            if (parsed.testReport == null && parsed.stdOut != null) {
                TestRunReportDto fallbackReport = tryParseReport(parsed.stdOut);
                if (fallbackReport != null) {
                    parsed.testReport = fallbackReport;
                }
            }
            return parsed;
        }

        return new RunResultDto() {{
            status = ExecutionStatus.FAILED_TO_EXECUTE;
            state = "Failed to parse test execution result.";
            exitCode = 1;
            wallTimeMs = TimeUnit.NANOSECONDS.toMillis(System.nanoTime() - startedAt);
            stdOut = stdout;
            stdErr = stderr;
        }};
    }

    private static RunResultDto tryParseSupervisorResult(String output) {
        if (output == null || output.isBlank()) return null;

        try {
            return objectMapper.readValue(output, RunResultDto.class);
        } catch (Exception ignored) {
            return null;
        }
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
        } catch (Exception ignored) {
            return null;
        }
    }

    private static String buildConsoleOutput(TestRunReportDto report, String stdout, String stderr) {
        if (report != null) {
            if (report.failedTests == null || report.failedTests.isEmpty()) return "";

            return report.failedTests.stream()
                    .map(f -> (f.name == null || f.name.isBlank() ? "unknown" : f.name) + ": " + (f.reason == null ? "" : f.reason))
                    .collect(Collectors.joining("\n"))
                    .trim();
        }

        StringBuilder combined = new StringBuilder();

        if (stdout != null && !stdout.isBlank()) {
            combined.append("--- STDOUT ---").append(System.lineSeparator());
            combined.append(stdout.trim()).append(System.lineSeparator());
        }

        if (stderr != null && !stderr.isBlank()) {
            combined.append("--- STDERR ---").append(System.lineSeparator());
            combined.append(stderr.trim()).append(System.lineSeparator());
        }

        return combined.toString().trim();
    }

    private static boolean isRequestSucceeded(RunResultDto runResult) {
        TestRunReportDto report = runResult.testReport;
        if (report == null) {
            return false;
        }
        return report.passedTests == report.totalTests;
    }

    private static List<String> checkForbiddenAPIs(String sourceCode) {
        CompilationUnit cu = StaticJavaParser.parse(sourceCode);
        List<String> violations = new ArrayList<>();

        cu.findAll(ObjectCreationExpr.class).forEach(expr -> {
            String typeName = expr.getType().getNameAsString();
            if ("ProcessBuilder".equals(typeName)) {
                violations.add("Usage of ProcessBuilder is forbidden.");
            }
        });

        cu.findAll(MethodCallExpr.class).forEach(method -> {
            String methodName = method.getNameAsString();
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

    private static CompileResult compileJavaFile(String javaFilePath, ByteArrayOutputStream errorOut) {
        try {
            JavaCompiler compiler = ToolProvider.getSystemJavaCompiler();

            if (compiler == null) {
                return new CompileResult(false, "JavaCompiler not available.");
            }

            int compileResult = compiler.run(
                    null,
                    null,
                    errorOut,
                    "-d", TMP_DIR,
                    javaFilePath
            );

            if (compileResult != 0) {
                return new CompileResult(false, errorOut.toString(StandardCharsets.UTF_8));
            }

            return new CompileResult(true, null);
        } catch (Exception e) {
            return new CompileResult(false, e.toString());
        }
    }

    private static String wrapUserCode(ProblemSolutionDto request, ManifestDto manifest) throws Exception {
        String userCode = request.UserSolution == null ? "" : request.UserSolution;
        long defaultTimeoutMs = 2000L;

        ITestWrapper wrapper = new JavaWrapper();
        String fullSource = wrapper.generateSource(
                manifest,
                request.LanguageCode == null ? LANG_CODE : request.LanguageCode,
                userCode,
                "SolutionContainer",
                defaultTimeoutMs
        );

        return fullSource.replaceFirst("(?m)^\\s*package\\s+[^;]+;\\s*", "");
    }

    private static UUID parseUuidOrRandom(String value) {
        try {
            return value != null ? UUID.fromString(value) : UUID.randomUUID();
        } catch (Exception ex) {
            return UUID.randomUUID();
        }
    }

    private static UUID parseUuidOrNull(String value) {
        try {
            return value != null ? UUID.fromString(value) : null;
        } catch (Exception ex) {
            return null;
        }
    }

    private static String readAll(java.io.InputStream inputStream) {
        try (BufferedReader reader = new BufferedReader(new InputStreamReader(inputStream, StandardCharsets.UTF_8))) {
            StringBuilder sb = new StringBuilder();
            String line;
            while ((line = reader.readLine()) != null) {
                sb.append(line).append('\n');
            }
            return sb.toString();
        } catch (IOException e) {
            return null;
        }
    }

    private static String safeGet(CompletableFuture<String> future) {
        try {
            return future.get();
        } catch (Exception e) {
            return null;
        }
    }

    private static String getJunitClasspath() {
        return System.getProperty("java.class.path");
    }

    private static void cleanupTempFiles() {
        try {
            Files.deleteIfExists(Paths.get(TMP_JAVA_FILE));
            Files.deleteIfExists(Paths.get(TMP_CLASS_FILE));
        } catch (IOException ignored) {
        }
    }

    private static void releaseResources() {
        cleanupTempFiles();
    }

    private class RequestHandler implements HttpHandler {
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
}
