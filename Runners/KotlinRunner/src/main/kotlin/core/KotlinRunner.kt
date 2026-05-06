package core

import com.fasterxml.jackson.annotation.JsonIgnoreProperties
import com.fasterxml.jackson.annotation.JsonProperty
import com.fasterxml.jackson.databind.ObjectMapper
import dto.CodeResponseDto
import dto.ExecutionResultDto
import dto.ProblemSolutionDto
import enums.ExecutionStatus
import enums.RequestStatus
import helpers.ITestWrapper
import helpers.KotlinWrapper
import helpers.ManifestParser
import manifest.ManifestDto
import org.jetbrains.kotlin.cli.common.ExitCode
import org.jetbrains.kotlin.cli.jvm.K2JVMCompiler
import org.slf4j.Logger
import java.io.ByteArrayOutputStream
import java.io.File
import java.io.PrintStream
import java.time.OffsetDateTime
import java.util.concurrent.CompletableFuture
import java.util.concurrent.TimeUnit
import java.util.concurrent.atomic.AtomicBoolean

class KotlinRunner(
    private val supervisorProcessBuilder: ProcessBuilder
) {
    private val objectMapper: ObjectMapper = JsonUtils.objectMapper

    private val languageCode: String = "kotlin"
    private val globalTimeoutMs: Long = 25_000L

    @JsonIgnoreProperties(ignoreUnknown = true)
    private data class RunRequestDto(
        @JsonProperty("executorFileName")
        var executorFileName: String = "",

        @JsonProperty("commandLineArguments")
        var commandLineArguments: List<String> = emptyList(),

        @JsonProperty("executableFileName")
        var executableFileName: String = "",

        @JsonProperty("maxProcessLifetime")
        var maxProcessLifetime: Int = 0
    )

    @JsonIgnoreProperties(ignoreUnknown = true)
    private data class RunResultDto(
        @JsonProperty("wallTimeMs")
        var wallTimeMs: Long = 0,

        @JsonProperty("cpuTimeUs")
        var cpuTimeUs: Long = 0,

        @JsonProperty("peakMemoryBytes")
        var peakMemoryBytes: Long = 0,

        @JsonProperty("testReport")
        var testReport: TestRunReportDto? = null,

        @JsonProperty("status")
        var status: ExecutionStatus = ExecutionStatus.NoStatus,

        @JsonProperty("state")
        var state: String = "",

        @JsonProperty("stdOut")
        var stdOut: String? = null,

        @JsonProperty("stdErr")
        var stdErr: String? = null,

        @JsonProperty("exitCode")
        var exitCode: Int = 0
    )

    @JsonIgnoreProperties(ignoreUnknown = true)
    private data class TestRunReportDto(
        @JsonProperty("totalTests")
        var totalTests: Int = 0,

        @JsonProperty("passedTests")
        var passedTests: Int = 0,

        @JsonProperty("failedTests")
        var failedTests: List<FailedTestDto> = emptyList()
    )

    @JsonIgnoreProperties(ignoreUnknown = true)
    private data class FailedTestDto(
        @JsonProperty("name")
        var name: String? = null,

        @JsonProperty("reason")
        var reason: String? = null
    )

    private data class CompileResult(
        val success: Boolean,
        val output: String?
    )

    fun run(solution: ProblemSolutionDto, logger: Logger): CodeResponseDto {
        val now = OffsetDateTime.now()
        logger.info("[Runner] Wrapping code for request [Id:${solution.RequestId}]")

        val testManifest: ManifestDto = ManifestParser.parse(solution.TestManifestJson, languageCode)
        val wrapped = wrapCode(solution, testManifest)

        val totalTests =
            (testManifest.sampleTests?.size ?: 0) +
                    (testManifest.advancedTests?.size ?: 0)

        try {
            logger.info("[Runner] Compiling code for request [Id:${solution.RequestId}]")

            val compiledRes = compileToTmpDir(wrapped, logger)
            if (!compiledRes.success) {
                logger.info("[Runner] Compilation failed for request [Id:${solution.RequestId}]")

                return CodeResponseDto().apply {
                    RequestId = solution.RequestId
                    UserId = solution.UserId
                    VersionId = solution.VersionId
                    Status = RequestStatus.Failed
                    Language = languageCode
                    UserSolution = solution.UserSolution
                    Result = ExecutionResultDto().apply {
                        Status = ExecutionStatus.CompileError
                        ExitCode = 1
                        ConsoleOutput = "Compilation failed: ${compiledRes.output}"
                        RequestSentAt = solution.SentAt
                        ResponseSentAt = now
                        PassedTests = 0
                        TotalTests = totalTests
                    }
                }
            }

            logger.info("[Runner] Running tests for request [Id:${solution.RequestId}]")

            val runResult = runTestsViaSupervisor(logger)

            val report = runResult.testReport

            logger.info(objectMapper.writeValueAsString(report));

            val passed = report?.passedTests ?: 0
            val actualTotal = report?.totalTests ?: totalTests
            val consoleSummary = buildConsoleOutput(report, runResult.stdOut, runResult.stdErr)

            val response = CodeResponseDto().apply {
                RequestId = solution.RequestId
                UserId = solution.UserId
                VersionId = solution.VersionId
                Language = languageCode
                UserSolution = solution.UserSolution
                Status = if (isRequestSucceeded(runResult)) RequestStatus.Succeeded else RequestStatus.Failed
                Result = ExecutionResultDto().apply {
                    Status = runResult.status
                    ExitCode = runResult.exitCode
                    PassedTests = passed
                    TotalTests = actualTotal
                    RequestSentAt = solution.SentAt
                    ResponseSentAt = now
                    CpuTimeUs = runResult.cpuTimeUs
                    PeakMemoryBytes = runResult.peakMemoryBytes
                    WallTimeMs = runResult.wallTimeMs
                    ConsoleOutput ="State: ${runResult.state}\n" + consoleSummary
                }
            }

            logger.info("[Runner] Finished request [Id:${solution.RequestId}] with status ${response.Status}")
            return response
        } finally {
            cleanTmpDir()
        }
    }

    private fun compileToTmpDir(code: String, logger: Logger): CompileResult {
        cleanTmpDir()

        val srcFile = File(tmpBaseDir, "UserProgram.kt").apply {
            writeText(code)
        }

        val args = arrayOf(
            "-no-stdlib",
            "-no-reflect",
            "-jvm-target", "21",
            "-classpath", runtimeClasspath,
            "-d", tmpBaseDir.absolutePath,
            srcFile.absolutePath
        )

        val compiler = K2JVMCompiler()
        val baos = ByteArrayOutputStream()
        val ps = PrintStream(baos)

        val exit = compiler.exec(ps, *args)
        val compileOutput = if (exit == ExitCode.OK) {
            null
        } else {
            baos.toString(Charsets.UTF_8.name()).also {
                logger.info("[Runner] Compiler output: $it")
            }
        }

        return CompileResult(exit == ExitCode.OK, compileOutput)
    }

    private fun runTestsViaSupervisor(logger: Logger): RunResultDto {
        val classpath = "${tmpBaseDir.absolutePath}${File.pathSeparator}$runtimeClasspath"

        val javaBin = System.getProperty("java.home") + File.separator + "bin" + File.separator + "java"

        val request = RunRequestDto(
            executorFileName = javaBin,
            commandLineArguments = listOf("-cp", classpath),
            executableFileName = "GeneratedTests",
            maxProcessLifetime = globalTimeoutMs.toInt()
        )

        val serializedRequest = objectMapper.writeValueAsString(request)

        val proc = supervisorProcessBuilder.start()

        val stdoutFuture = CompletableFuture.supplyAsync {
            proc.inputStream.bufferedReader().use { it.readText() }
        }
        val stderrFuture = CompletableFuture.supplyAsync {
            proc.errorStream.bufferedReader().use { it.readText() }
        }

        proc.outputStream.bufferedWriter().use { writer ->
            writer.write(serializedRequest)
            writer.flush()
        }

        val startedAt = System.nanoTime()
        val finished = proc.waitFor(globalTimeoutMs, TimeUnit.MILLISECONDS)

        if (!finished) {
            try {
                proc.destroyForcibly()
            } catch (_: Exception) {
            }

            val stdout = safeGet(stdoutFuture)
            val stderr = safeGet(stderrFuture)

            return RunResultDto(
                status = ExecutionStatus.TimedOut,
                state = "Supervisor timed out.",
                exitCode = 124,
                wallTimeMs = TimeUnit.NANOSECONDS.toMillis(System.nanoTime() - startedAt),
                stdOut = stdout,
                stdErr = stderr
            )
        }

        val stdout = safeGet(stdoutFuture)
        val stderr = safeGet(stderrFuture)

        val parsed = tryParseSupervisorResult(stdout, logger)
        if (parsed != null) {
            return parsed
        }

        logger.info("[Runner] Failed to parse supervisor output. stdout=$stdout stderr=$stderr")

        return RunResultDto(
            status = ExecutionStatus.FailedToExecute,
            state = "Failed to parse test execution result.",
            exitCode = 1,
            wallTimeMs = TimeUnit.NANOSECONDS.toMillis(System.nanoTime() - startedAt),
            stdOut = stdout,
            stdErr = stderr
        )
    }

    private fun tryParseSupervisorResult(output: String?, logger: Logger): RunResultDto? {
        if (output.isNullOrBlank()) return null
        return try {
            objectMapper.readValue(output, RunResultDto::class.java)
        } catch (e: Exception) {
            logger.info("[Runner] JSON parse failed: ${e.message}")
            null
        }
    }

    private fun buildConsoleOutput(
        report: TestRunReportDto?,
        stdout: String?,
        stderr: String?
    ): String {
        if (report != null) {
            if (report.failedTests.isEmpty()) return ""

            return report.failedTests.joinToString("\n") { f ->
                "${f.name ?: "unknown"}: ${f.reason ?: ""}"
            }.trim()
        }

        val combined = StringBuilder()

        if (!stdout.isNullOrBlank()) {
            combined.appendLine("--- STDOUT ---")
            combined.appendLine(stdout.trim())
        }

        if (!stderr.isNullOrBlank()) {
            combined.appendLine("--- STDERR ---")
            combined.appendLine(stderr.trim())
        }

        return combined.toString().trim()
    }

    private fun isRequestSucceeded(runResult: RunResultDto): Boolean {
        val report = runResult.testReport
        if (report == null)
            return false;

        return report.passedTests == report.totalTests
    }

    private fun wrapCode(solution: ProblemSolutionDto, manifest: ManifestDto): String {
        val userCode = solution.UserSolution ?: ""
        val defaultTimeoutMs = 2000L

        val wrapper: ITestWrapper = KotlinWrapper
        val fullSource: String = wrapper.generateSource(
            manifest,
            solution.LanguageCode ?: languageCode,
            userCode,
            "SolutionContainer",
            defaultTimeoutMs
        )

        return fullSource.replaceFirst(
            Regex("(?m)^\\s*package\\s+[^;]+;\\s*"),
            ""
        )
    }


    private fun cleanTmpDir() {
        tmpBaseDir.listFiles()?.forEach { it.deleteRecursively() }
    }

    private fun safeGet(future: CompletableFuture<String>): String? {
        return try {
            future.get()
        } catch (_: Exception) {
            null
        }
    }

    companion object {
        private const val junitClasspath: String = "/libs/junit-4.13.2.jar:/libs/hamcrest-core-1.3.jar"

        private val runtimeClasspath: String =
            System.getProperty("java.class.path") + File.pathSeparator + junitClasspath

        private val tmpBaseDir: File = File(
            System.getProperty("java.io.tmpdir"),
            "kotlinc"
        ).apply { mkdirs() }
    }
}