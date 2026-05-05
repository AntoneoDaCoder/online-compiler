package core

import com.fasterxml.jackson.annotation.JsonIgnoreProperties
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
import java.util.concurrent.TimeUnit
import java.util.stream.Collectors


class KotlinRunner {

    private val REPORT_BEGIN_MARKER = "__TEST_REPORT_BEGIN__"
    private val REPORT_END_MARKER = "__TEST_REPORT_END__"

    private val objectMapper: ObjectMapper = JsonUtils.objectMapper;

    @JsonIgnoreProperties(ignoreUnknown = true)
    class TestRunReportDto {
        var totalTests: Int = 0
        var passedTests: Int = 0
        var failedTests: List<FailedTestDto> = ArrayList()
    }

    @JsonIgnoreProperties(ignoreUnknown = true)
    class FailedTestDto {
        var name: String? = null
        var reason: String? = null
    }

    private fun tryParseReport(output: String?): TestRunReportDto? {
        if (output == null || output.isBlank()) return null

        var begin = output.indexOf(REPORT_BEGIN_MARKER)
        if (begin < 0) return null
        begin += REPORT_BEGIN_MARKER.length

        val end = output.indexOf(REPORT_END_MARKER, begin)
        if (end < 0 || end <= begin) return null

        val json = output.substring(begin, end).trim { it <= ' ' }
        if (json.isEmpty()) return null

        return try {
            objectMapper.readValue(json, TestRunReportDto::class.java)
        } catch (e: Exception) {
            null
        }
    }

    private fun buildConsoleOutput(report: TestRunReportDto?, rawOutput: String?): String {
        if (report != null) {
            if (report.failedTests.isNullOrEmpty()) return ""
            return report.failedTests.joinToString("\n") { f ->
                "${f.name ?: "unknown"}: ${f.reason ?: ""}"
            }
        }

        return extractLegacyFailureSummary(rawOutput)
    }

    private fun extractLegacyFailureSummary(output: String?): String {
        if (output.isNullOrBlank()) return ""

        val lines = output.lines()
        val result = mutableListOf<String>()

        var i = 0
        while (i < lines.size) {
            val line = lines[i].trim()
            val header = Regex("""^\d+\)\s*(.+)$""").find(line)
            if (header != null) {
                val testName = header.groupValues[1].substringBefore("(").trim().ifBlank { "unknown" }

                var reason = ""
                var j = i + 1
                while (j < lines.size) {
                    val candidate = lines[j].trim()
                    if (candidate.isBlank()) {
                        j++
                        continue
                    }
                    if (candidate.startsWith("at ") ||
                        candidate.startsWith("Caused by:") ||
                        candidate.startsWith("FAILURES!!!") ||
                        candidate.startsWith("Tests run:") ||
                        candidate.startsWith("Time:")
                    ) break

                    reason = candidate
                    break
                }

                result.add("$testName: $reason".trim())
            }
            i++
        }

        return result.joinToString("\n")
    }

    companion object {
        // JUnit jars — пути в образе/контейнере
        private const val junitClasspath: String = "/libs/junit-4.13.2.jar:/libs/hamcrest-core-1.3.jar"

        private const val languageCode: String = "kotlin";

        private val runtimeClasspath: String =
            System.getProperty("java.class.path") + File.pathSeparator + junitClasspath

        private val tmpBaseDir: File = File(
            System.getProperty("java.io.tmpdir"),
            "kotlinc"
        ).apply { mkdirs() }

        private fun cleanTmpDir() {
            tmpBaseDir.listFiles()?.forEach { it.deleteRecursively() }
        }

    }


    fun run(solution: ProblemSolutionDto, logger: Logger): CodeResponseDto {
        val now = OffsetDateTime.now()
        logger.info("[Runner] Wrapping code for request [Id:${solution.RequestId}]")

        val testManifest:ManifestDto = ManifestParser.parse(solution.TestManifestJson, languageCode);

        val wrapped = wrapCode(solution,testManifest)

        logger.info("[Runner] Compiling code for request [Id:${solution.RequestId}]")

        val compiledRes = compileToTmpDir(wrapped, logger)

        val total = testManifest.sampleTests!!.size + testManifest.advancedTests!!.size;

        if (!compiledRes.first) {
            logger.info("[Runner] Compilation failed for request [Id:${solution.RequestId}]")
            return CodeResponseDto().apply {
                RequestId =solution.RequestId
                UserId = solution.UserId
                VersionId = solution.VersionId
                Status = RequestStatus.Failed
                Language = languageCode
                UserSolution = solution.UserSolution
                Result = ExecutionResultDto().apply {
                    Status = ExecutionStatus.CompileError
                    ExitCode = 1
                    ConsoleOutput = "Compilation failed: ${compiledRes.second}"
                    RequestSentAt = solution.SentAt
                    ResponseSentAt = now
                     this.PassedTests =0
                    this.TotalTests = total
                }
            }
        }

        logger.info("[Runner] Running tests for request [Id:${solution.RequestId}]")
        val (exitCode, output) = runTestsInProcess(logger)

        val report = tryParseReport(output)
        val passed = report?.passedTests ?: 0
        val actualTotal = report?.totalTests ?: total

        val consoleSummary = buildConsoleOutput(report, output)

        val (status, reqStatus) = when {
            report != null && report.failedTests.isNullOrEmpty() ->
                ExecutionStatus.Succeeded to RequestStatus.Succeeded

            report != null ->
                ExecutionStatus.FailedToExecute to RequestStatus.Failed

            detectTimeout(output) || exitCode == 124 ->
                ExecutionStatus.TimedOut to RequestStatus.Failed

            output.contains("Exception", ignoreCase = true) || output.contains("Error", ignoreCase = true) ->
                ExecutionStatus.RuntimeError to RequestStatus.Failed

            else ->
                ExecutionStatus.NoStatus to RequestStatus.Failed
        }

        logger.info("[Runner] Finished request [Id:${solution.RequestId}] with status $status")

        return CodeResponseDto().apply {
            RequestId = solution.RequestId
            UserId = solution.UserId
            VersionId = solution.VersionId
            Status = reqStatus
            Language = languageCode
            UserSolution = solution.UserSolution
            Result = ExecutionResultDto().apply {
                this.Status = status
                this.ExitCode = exitCode
                this.ConsoleOutput = consoleSummary
                this.PassedTests = passed
                this.RequestSentAt = solution.SentAt
                this.ResponseSentAt = now
                this.PassedTests = passed
                this.TotalTests = actualTotal
            }
        }
    }

    fun compileToTmpDir(code: String, logger: Logger): Pair<Boolean,String?> {
        cleanTmpDir()
        val srcFile = File(tmpBaseDir, "UserProgram.kt").apply { writeText(code) }

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
        var compileOutput: String? = null
        if (exit != ExitCode.OK) {
            compileOutput = baos.toString()
            logger.info("[Runner] Compiler output: $compileOutput")
        }
        return Pair(exit == ExitCode.OK, compileOutput);
    }

    private fun runTestsInProcess(logger: Logger): Pair<Int, String> {
        val globalTimeoutMs = 25_000L
        val classpath = "${tmpBaseDir.absolutePath}${File.pathSeparator}$runtimeClasspath"
        val javaBin = System.getProperty("java.home") + File.separator + "bin" + File.separator + "java"
        val cmd = listOf(
            javaBin,
            "-cp",
            classpath,
            "GeneratedTests"
        )
        val processBuilder = ProcessBuilder(cmd).redirectErrorStream(true)
        val process = processBuilder.start()
        val finished = process.waitFor(globalTimeoutMs, TimeUnit.MILLISECONDS)
        return if (!finished) {
            process.destroyForcibly()
            124 to "Execution timed out after ${globalTimeoutMs}ms"
        } else {
            val output = process.inputStream.bufferedReader().readText()
            tmpBaseDir.listFiles()?.forEach { it.deleteRecursively() }
            process.exitValue() to output
        }
    }

    private fun wrapCode(solution: ProblemSolutionDto,manifest:ManifestDto): String {
        val userCode = if (solution.UserSolution == null) "" else solution.UserSolution
        val defaultTimeoutMs = 2000L

        val wrapper: ITestWrapper = KotlinWrapper
        val fullSource: String = wrapper.generateSource(
            manifest,
            if (solution.LanguageCode == null) languageCode else solution.LanguageCode,
            userCode,
            "SolutionContainer",
            defaultTimeoutMs
        )

        // keep previous behavior: strip package declarations
        return fullSource.replaceFirst("(?m)^\\s*package\\s+[^;]+;\\s*".toRegex(), "")
    }


    private fun detectTimeout(output: String): Boolean {
        if (output.isBlank()) return false
        val lower = output.lowercase()
        if (lower.contains("timed out") || lower.contains("test timed out") || lower.contains("junit.framework.AssertionFailedError: test timed out")) return true
        if (lower.contains("testtimeoutexception") || lower.contains("testtimedoutexception")) return true
        return false
    }
}
