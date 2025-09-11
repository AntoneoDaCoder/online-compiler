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
import java.time.LocalDateTime;
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
import com.mems.Helpers.JsonUtils;
import com.mems.Shared.DTOs.CodeResponseDto;
import com.mems.Shared.DTOs.ExecutionResultDto;
import com.mems.Shared.DTOs.ProblemSolutionDto;
import com.mems.Shared.Enums.ExecutionStatus;
import com.mems.Shared.Enums.RequestStatus;
import com.mems.Shared.Models.AdditionalDefinition;
import com.mems.Shared.Models.TestCase;

public class Runner 
{
    private static final String TMP_DIR = System.getProperty("java.io.tmpdir");
    private static final String TMP_CLASS_NAME = "UserProgram";
    private static final String TMP_JAVA_FILE = TMP_DIR + "/UserProgram.java";
    private static final String TMP_CLASS_FILE = TMP_DIR + "/UserProgram.class";
    private static final String API_CALLBACK_URL = "http://api-server.default.svc.cluster.local:8080/api/jobs/complete";
    private static final int MAX_PROCESS_LIFETIME_MS = 25000;
    private static final int RUNNER_PORT = 5000;
    
    private static final String BOILERPLATE_IMPORTS = """
        import java.util.*;
        import java.util.stream.*;
        import java.io.*;
        import org.junit.*;
        import org.junit.runner.*;
        import org.junit.runners.*;
        import static org.junit.Assert.*;
        import org.junit.internal.*;
        import org.junit.runner.notification.Failure;
        """;
    
    private static final HttpClient httpClient = HttpClient.newHttpClient();
    private static final ObjectMapper objectMapper = JsonUtils.getObjectMapper();

    public static void main(String[] args) throws Exception {
        Runtime.getRuntime().addShutdownHook(new Thread(Runner::releaseResources));

        if (args.length > 0 && args[0].equals("--server")) {
            startServer();
        } else if (args.length > 0 && args[0].equals("--once")) {
            runOnce();
        } else {
            System.out.println("Usage: java -jar runner.jar [--server | --once]");
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
            System.out.println(responseJson);

        } catch (Exception e) {
            System.err.println("[JavaRunner] CLI mode failed: " + e);
            e.printStackTrace();
        }
    }

    private static CodeResponseDto executeUserCodeOnce(ProblemSolutionDto request) {
        CodeResponseDto response = new CodeResponseDto();
        response.requestId = request.requestId;
        response.language = "java";
        response.result = new ExecutionResultDto();
        response.result.requestSentAt = request.sentAt;
        response.result.responseSentAt = LocalDateTime.now();

        try {
            String fullCode = wrapUserCode(request);
            Files.writeString(Paths.get(TMP_JAVA_FILE), fullCode);

            List<String> violations = checkForbiddenAPIs(fullCode);

            if (!violations.isEmpty()) {
                System.err.println("[JavaRunner] Forbidden API usage detected:");
                response.status = RequestStatus.FAILED;
                response.result.status = ExecutionStatus.CANCELLED;
                response.result.exitCode = 2;
                response.result.consoleOutput = String.join("\n", violations);
                return response;
            }

            ByteArrayOutputStream errorOutput = new ByteArrayOutputStream();
            boolean compiled = compileJavaFile(TMP_JAVA_FILE, errorOutput);

            if (!compiled) {
                response.status = RequestStatus.FAILED;
                response.result.status = ExecutionStatus.COMPILE_ERROR;
                response.result.exitCode = 1;

                String fullError = errorOutput.toString(StandardCharsets.UTF_8);
                int index = fullError.indexOf("error:");
                if (index != -1) {
                    fullError = fullError.substring(index);
                }

                response.result.consoleOutput = fullError;
                return response;
            }

            String separator = System.getProperty("path.separator");
            String classpath = TMP_DIR + separator + getJunitClasspath();

            ProcessBuilder pb = new ProcessBuilder("java", "-cp", classpath, TMP_CLASS_NAME);
            pb.redirectErrorStream(true);

            Process process = pb.start();
            StringBuilder output = new StringBuilder();

            Thread outputReader = new Thread(() -> {
                try (BufferedReader reader = new BufferedReader(new InputStreamReader(process.getInputStream()))) {
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
                response.status = RequestStatus.FAILED;
                response.result.status = ExecutionStatus.TIMED_OUT;
                response.result.exitCode = 124;
                response.result.consoleOutput = "Execution timed out";
            } else {
                response.result.exitCode = process.exitValue();

                if (process.exitValue() == 0) {
                    response.status = RequestStatus.SUCCEEDED;
                    response.result.status = ExecutionStatus.SUCCEEDED;
                    response.result.consoleOutput = output.toString();
                } else {
                    response.status = RequestStatus.FAILED;
                    response.result.status = parseTestResults(output.toString());
                    response.result.consoleOutput = output.toString();
                }
            }

        } catch (Exception e) {
            response.status = RequestStatus.FAILED;
            response.result.status = ExecutionStatus.RUNTIME_ERROR;
            response.result.consoleOutput = e.toString();
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
                String requestBody = new String(exchange.getRequestBody().readAllBytes());
                ProblemSolutionDto request = objectMapper.readValue(requestBody, ProblemSolutionDto.class);
                
                System.out.println("[JavaRunner] Received request [Id:" + request.requestId + "]");

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

        response.requestId = request.requestId;
        response.language = "java";
        response.result = new ExecutionResultDto();
        response.result.requestSentAt = request.sentAt;
        
        try {
            String fullCode = wrapUserCode(request);
            Files.writeString(Paths.get(TMP_JAVA_FILE), fullCode);

            List<String> violations = checkForbiddenAPIs(fullCode);

            if (violations.isEmpty()) {
                System.out.println("Code is safe.");
            } else {
                System.out.println("Forbidden API usage detected:");
                response.status = RequestStatus.FAILED;
                response.result.status = ExecutionStatus.CANCELLED;
                response.result.exitCode = 2;
                response.result.consoleOutput = String.join("\n", violations);
                
                notifyJobManager(response);

                return;
            }

            ByteArrayOutputStream errorOutput = new ByteArrayOutputStream();
            boolean compiled = compileJavaFile(TMP_JAVA_FILE, errorOutput);
            
            if (!compiled) {
                response.status = RequestStatus.FAILED;
                response.result.status = ExecutionStatus.COMPILE_ERROR;
                response.result.exitCode = 1;

                String fullError = errorOutput.toString(StandardCharsets.UTF_8);
                int index = fullError.indexOf("error:");

                if (index != -1) {
                    fullError = fullError.substring(index);
                }
                
                response.result.consoleOutput = fullError;
                
                return;
            }
            
            String separator = System.getProperty("path.separator");
            String classpath = TMP_DIR + separator + getJunitClasspath();

            ProcessBuilder pb = new ProcessBuilder("java", "-cp", classpath, TMP_CLASS_NAME);
            pb.redirectErrorStream(true);

            Process process = pb.start();
            StringBuilder output = new StringBuilder();
            Thread outputReader = new Thread(() -> {
                try (BufferedReader reader = new BufferedReader(new InputStreamReader(process.getInputStream()))) {
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

            System.out.println("[JavaRunner] Process wait completed.");
            
            if (!completed) {
                process.destroyForcibly();
                response.status = RequestStatus.FAILED;
                response.result.status = ExecutionStatus.TIMED_OUT;
                response.result.exitCode = 124;
                response.result.consoleOutput = "Execution timed out";
            } else {
                response.result.exitCode = process.exitValue();
                
                if (process.exitValue() == 0) {
                    response.status = RequestStatus.SUCCEEDED;
                    response.result.status = ExecutionStatus.SUCCEEDED;

                    System.out.println("[JavaRunner] Code successfully executed");
                } else {
                    System.out.print(output);
                    response.status = RequestStatus.FAILED;
                    response.result.status = parseTestResults(output.toString());
                    response.result.consoleOutput = extractFailedTestNames(output.toString());
                    System.out.println("[JavaRunner] Status code is different from 0");
                }
            }
            
        } catch (Exception e) {
            response.status = RequestStatus.FAILED;
            response.result.status = ExecutionStatus.RUNTIME_ERROR;
            response.result.consoleOutput = e.toString();

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
    
    private static String wrapUserCode(ProblemSolutionDto request) {
        StringBuilder sb = new StringBuilder(BOILERPLATE_IMPORTS);
        sb.append("public class UserProgram {");

        for (AdditionalDefinition def : request.problem.additionalDefinitions) {
            sb.append(def.value).append("\n");
        }
        sb.append(request.code).append("\n");
        sb.append("""
            
                public static void main(String[] args) {
                    try {
                        JUnitCore junit = new JUnitCore();
                        junit.addListener(new TextListener(System.out));

                        Result result = junit.run(GeneratedTests.class);

                        for (Failure failure : result.getFailures()) {
                            System.err.println("[TEST FAILED] " + failure.getTestHeader());
                            System.err.println(failure.getMessage());
                        }

                        if (result.wasSuccessful()) {
                            System.exit(0);
                        } else {
                            System.exit(1);
                        }
                    } catch (Throwable t) {
                        t.printStackTrace();
                        System.exit(2);
                    }
                }

                @RunWith(JUnit4.class)
                public static class GeneratedTests {
                    public GeneratedTests() {}

            """);
        
        for (TestCase testCase : request.problem.testCases) {
            sb.append(String.format("""
                @Test(timeout = %d)
                public void %s() throws Exception {
                    %s
                    %s
                    %s
                }
                """, 
                request.maxAllowedTimeInMilliseconds,
                testCase.name,
                testCase.testInitialization,
                testCase.inputExpression,
                testCase.outputExpression));
        }
        
        sb.append("}");
        sb.append("}");
        
        String result = sb.toString();

        // remove package
        return result.replaceFirst("(?m)^\\s*package\\s+[^;]+;\\s*", ""); 
    }
    
    private static ExecutionStatus parseTestResults(String output) {
        if (output.contains("test timed out")) {
            return ExecutionStatus.TIMED_OUT;
        } else if (output.contains("FAILURES!!!")) {
            return ExecutionStatus.FAILED_TO_EXECUTE;
        } else if (output.contains("Exception") || output.contains("at ")) {
            return ExecutionStatus.RUNTIME_ERROR;
        }
        return ExecutionStatus.NO_STATUS;
    }
    
    private static String extractFailedTestNames(String output) {
        return Arrays.stream(output.split("\n"))
            .filter(line -> line.startsWith("[TEST FAILED]"))
            .map(line -> {
                int start = "[TEST FAILED] ".length();
                int end = line.indexOf('(');
                if (end == -1) end = line.length();
                return line.substring(start, end).trim();
            })
            .collect(Collectors.joining("\n"));
    }
    
    private static void notifyJobManager(CodeResponseDto response) {
        response.result.responseSentAt = LocalDateTime.now();
        
        try {
            HttpRequest request = HttpRequest.newBuilder()
                .uri(URI.create(API_CALLBACK_URL))
                .header("Content-Type", "application/json")
                .POST(HttpRequest.BodyPublishers.ofString(objectMapper.writeValueAsString(response)))
                .build();
                
            HttpResponse<String> httpResponse = httpClient.send(request, HttpResponse.BodyHandlers.ofString());

            System.out.println("[JavaRunner] Sent response [Id:" + response.requestId + "]");
            System.out.println("[JavaRunner] HTTP Status code: " + httpResponse.statusCode());
        } catch (Exception e) {
            System.err.println("[JavaRunner] Failed to send response [Id:" + response.requestId + "]: " + e);
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