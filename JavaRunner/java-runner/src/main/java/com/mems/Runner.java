package com.mems;

import java.io.BufferedReader;
import java.io.IOException;
import java.io.InputStreamReader;
import java.io.PrintWriter;
import java.io.StringWriter;
import java.lang.reflect.InvocationTargetException;
import java.net.InetSocketAddress;
import java.net.URI;
import java.net.http.HttpClient;
import java.net.http.HttpRequest;
import java.net.http.HttpResponse;
import java.nio.file.Files;
import java.nio.file.Path;
import java.nio.file.Paths;
import java.time.LocalDateTime;
import java.util.Arrays;
import java.util.UUID;
import java.util.concurrent.CompletableFuture;
import java.util.concurrent.TimeUnit;
import java.util.stream.Collectors;

import javax.tools.Diagnostic;
import javax.tools.DiagnosticCollector;
import javax.tools.JavaCompiler;
import javax.tools.JavaCompiler.CompilationTask;
import javax.tools.JavaFileObject;
import javax.tools.SimpleJavaFileObject;
import javax.tools.ToolProvider;
import com.sun.net.httpserver.HttpExchange;
import com.sun.net.httpserver.HttpHandler;
import com.sun.net.httpserver.HttpServer;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.mems.Helpers.JsonUtils;
import com.mems.Shared.DTOs.CodeResponseDto;
import com.mems.Shared.DTOs.ExecutionResultDto;
import com.mems.Shared.DTOs.ProblemSolutionDto;
import com.mems.Shared.Enums.ExecutionStatus;
import com.mems.Shared.Enums.RequestStatus;
import com.mems.Shared.Models.Problem;
import com.mems.Shared.Models.TestCase;

public class Runner 
{
    private static final String TMP_DIR = System.getProperty("java.io.tmpdir");
    private static final String TMP_CLASS_NAME = "UserProgram";
    private static final String TMP_JAVA_FILE = TMP_DIR + "/UserProgram.java";
    private static final String TMP_CLASS_FILE = TMP_DIR + "/UserProgram.class";
    private static final String API_CALLBACK_URL = "http://api-server.default.svc.cluster.local:8080/api/jobs/complete";
    private static final int MAX_PROCESS_LIFETIME_MS = 25000;
    
    private static final String BOILERPLATE_IMPORTS = """
        import java.util.*;
        import java.util.stream.*;
        import org.junit.*;
        import org.junit.runner.*;
        import org.junit.runners.*;
        import static org.junit.Assert.*;
        import org.junit.runner.notification.Failure;
        """;
    
    private static final HttpClient httpClient = HttpClient.newHttpClient();
    private static final ObjectMapper objectMapper = JsonUtils.getObjectMapper();
    
    public static void main(String[] args) throws Exception {
        ProblemSolutionDto request = new ProblemSolutionDto();
        request.requestId = UUID.randomUUID();
        request.code = """
            class SimpleTest {
                public static int add(int a, int b) {
                    return a - b;
                }
            }
            """;
        
        request.problem = new Problem();
        request.problem.testCases = Arrays.asList(
            new TestCase(
                "testSuccess", 
                "", 
                "assertEquals(5, SimpleTest.add(2, 3));",
                ""
            ),
            new TestCase(
                "testRuntimeError", 
                "",                            
                "throw new RuntimeException(\"runtime error\");",  
                ""
            )
        );
        
        request.maxAllowedTimeInMilliseconds = 1000;
        request.sentAt = LocalDateTime.now();
        
        executeUserCode(request);
        
        Thread.sleep(3000);
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
                System.err.println("Error processing request: " + e);
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
            
            if (!compileJavaFile(TMP_JAVA_FILE)) {
                response.status = RequestStatus.FAILED;
                response.result.status = ExecutionStatus.COMPILE_ERROR;
                response.result.exitCode = 1;
                response.result.consoleOutput = "Compilation failed";
                //notifyJobManager(response);
                return;
            }
            
            // Execute the compiled code
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
                } else {
                    System.out.println(output.toString());
                    response.status = RequestStatus.FAILED;
                    response.result.status = parseTestResults(output.toString());
                    response.result.consoleOutput = extractFailedTestNames(output.toString());
                }
            }
            
        } catch (Exception e) {
            response.status = RequestStatus.FAILED;
            response.result.status = ExecutionStatus.RUNTIME_ERROR;
            response.result.consoleOutput = e.toString();
        } finally {
            cleanupTempFiles();
            //notifyJobManager(response);
        }
    }
    
    private static boolean compileJavaFile(String javaFilePath) {
        JavaCompiler compiler = ToolProvider.getSystemJavaCompiler();
        int compileResult = compiler.run(
            null, 
            null, 
            null, 
            "-d", TMP_DIR,
            Paths.get(TMP_DIR, "UserProgram.java").toString()
        );

        if (compileResult != 0) {
            return false;
        }

        return true;
    }
    
    private static String wrapUserCode(ProblemSolutionDto request) {
        StringBuilder sb = new StringBuilder(BOILERPLATE_IMPORTS);
        
        sb.append(request.problem.additionalDefinitions).append("\n");
        sb.append(request.code).append("\n");
        
        sb.append("""
            public class UserProgram {
                public static void main(String[] args) {
                    try {
                        Result result = JUnitCore.runClasses(GeneratedTests.class);

                        for (Failure failure : result.getFailures()) {
                            System.out.println("Test failed: " + failure.toString());
                        }

                        System.out.println(result.wasSuccessful() ? "All tests passed" : "Some tests failed");
                        System.exit(result.wasSuccessful() ? 0 : 1);
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
        if (output.contains("Test timed out")) {
            return ExecutionStatus.TIMED_OUT;
        } else if (output.contains("AssertionError") || output.contains("FAILURE")) {
            return ExecutionStatus.FAILED_TO_EXECUTE;
        } else if (output.contains("runtime")) {
            return ExecutionStatus.RUNTIME_ERROR;
        }
        return ExecutionStatus.NO_STATUS;
    }
    
    private static String extractFailedTestNames(String output) {
        return Arrays.stream(output.split("\n"))
            .filter(line -> line.contains(") test") && line.contains("FAILED"))
            .map(line -> line.substring(0, line.indexOf("(")).trim())
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
            
            httpClient.send(request, HttpResponse.BodyHandlers.ofString());
            System.out.println("[JavaRunner] Sent response [Id:" + response.requestId + "]");
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