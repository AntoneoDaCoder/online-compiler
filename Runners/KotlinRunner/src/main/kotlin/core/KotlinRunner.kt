package core

import dto.CodeResponseDto
import dto.ExecutionResultDto
import dto.ProblemSolutionDto
import enums.ExecutionStatus
import enums.RequestStatus
import helpers.ManifestParser
import manifest.*
import org.jetbrains.kotlin.cli.common.ExitCode
import org.jetbrains.kotlin.cli.jvm.K2JVMCompiler
import org.slf4j.Logger
import java.io.ByteArrayOutputStream
import java.io.File
import java.io.PrintStream
import java.time.OffsetDateTime
import java.util.*
import java.util.concurrent.TimeUnit
import java.util.regex.Pattern
import helpers.*

class KotlinRunner {
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

        private val passedRegex = Pattern.compile("PassedTests\\s*[:=]\\s*(\\d+)", Pattern.CASE_INSENSITIVE)
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

        val passed = parsePassedTests(output)

        // parse failures/timeouts/exceptions
        val failures = parseJUnitFailures(output)
        val timedOutDetected = detectTimeout(output) || exitCode == 124

        val (status, reqStatus, consoleSummary) = when {
            timedOutDetected -> Triple(ExecutionStatus.TimedOut, RequestStatus.Failed, buildTimeoutSummary(output, failures))
            failures.isNotEmpty() -> Triple(ExecutionStatus.FailedToExecute, RequestStatus.Failed, buildFailuresSummary(failures, output))
            exitCode == 0 -> Triple(ExecutionStatus.Succeeded, RequestStatus.Succeeded, buildSuccessSummary(output, passed))
            output.contains("Exception") || output.contains("Error") -> Triple(ExecutionStatus.RuntimeError, RequestStatus.Failed, buildRuntimeErrorSummary(output))
            else -> Triple(ExecutionStatus.NoStatus, RequestStatus.Failed, buildRuntimeErrorSummary(output))
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
                this.TotalTests = total
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
            "org.junit.runner.JUnitCore",
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


    private data class FailureInfo(val testName: String, val message: String)

    private fun parseJUnitFailures(output: String): List<FailureInfo> {
        val res = mutableListOf<FailureInfo>()
        if (output.isBlank()) return res

        val headerRegex = Regex("""(?m)^\s*\d+\)\s*(.+)$""", RegexOption.MULTILINE)
        val matches = headerRegex.findAll(output).toList()
        if (matches.isEmpty()) return res

        for ((i, m) in matches.withIndex()) {
            val start = m.range.first
            val end = if (i + 1 < matches.size) matches[i + 1].range.first else output.length
            val block = output.substring(start, end).trim()
            // First line is header "1) testMethod(ClassName)"
            val headerLine = m.groupValues[1].trim()
            val nameMatch = Regex("""^([^\(]+)\(([^)]+)\)""").find(headerLine)
            val testName = if (nameMatch != null) nameMatch.groupValues[1].trim() else headerLine

            // extract message lines — skip header line, take lines until stacktrace 'at ' appears
            val blockLines = block.split(Regex("\r?\n"))
            val afterHeader = if (blockLines.size > 1) blockLines.subList(1, blockLines.size) else emptyList()
            val messageLines = mutableListOf<String>()
            for (ln in afterHeader) {
                val trimmed = ln.trim()
                if (trimmed.isEmpty()) continue
                if (trimmed.startsWith("at ") || trimmed.startsWith("\tat ") || trimmed.startsWith("Caused by:")) break
                // skip lines that are just indentation markers
                messageLines.add(trimmed)
                if (messageLines.size >= 3) break // limit message length
            }
            val message = if (messageLines.isNotEmpty()) messageLines.joinToString(" | ") else {
                // fallback: try to find "AssertionError" or first non-empty after header
                val fallback = afterHeader.firstOrNull { it.trim().isNotEmpty() }?.trim() ?: ""
                fallback
            }
            res.add(FailureInfo(testName, message))
        }
        return res
    }

    private fun detectTimeout(output: String): Boolean {
        if (output.isBlank()) return false
        val lower = output.lowercase()
        if (lower.contains("timed out") || lower.contains("test timed out") || lower.contains("junit.framework.AssertionFailedError: test timed out")) return true
        if (lower.contains("testtimeoutexception") || lower.contains("testtimedoutexception")) return true
        return false
    }

    private fun buildFailuresSummary(failures: List<FailureInfo>, fullOut: String): String {
        val sb = StringBuilder()
        sb.appendLine("FAILED TESTS:")
        for (f in failures) {
            sb.appendLine("${f.testName}: ${f.message}")
        }
        sb.appendLine("--- FULL OUTPUT ---")
        sb.appendLine(fullOut)
        return sb.toString()
    }

    private fun buildTimeoutSummary(fullOut: String, failures: List<FailureInfo>): String {
        val sb = StringBuilder()
        if (failures.isNotEmpty()) {
            sb.appendLine("TIMEOUT and FAILURES (mixed):")
            for (f in failures) sb.appendLine("${f.testName}: ${f.message}")
        } else {
            sb.appendLine("TIMEOUT: test process exceeded allowed time")
        }
        sb.appendLine("--- FULL OUTPUT ---")
        sb.appendLine(fullOut)
        return sb.toString()
    }

    private fun buildRuntimeErrorSummary(fullOut: String): String {
        val sb = StringBuilder()
        sb.appendLine("RUNTIME ERROR or UNEXPECTED OUTPUT")
        sb.appendLine("--- FULL OUTPUT ---")
        sb.appendLine(fullOut)
        return sb.toString()
    }

    private fun buildSuccessSummary(fullOut: String, passed: Int): String {
        val sb = StringBuilder()
        sb.appendLine("All tests passed (or no failures detected).")
        sb.appendLine("PassedTests: $passed")
        sb.appendLine("--- FULL OUTPUT ---")
        sb.appendLine(fullOut)
        return sb.toString()
    }

    private val passedPatterns = listOf(
        Pattern.compile("PassedTests\\s*[:=]\\s*(\\d+)", Pattern.CASE_INSENSITIVE),
        Pattern.compile("OK \\((\\d+) tests?\\)"), // JUnit summary
        Pattern.compile("Tests run:\\s*(\\d+)", Pattern.CASE_INSENSITIVE) // другой формат
    )

    private fun parsePassedTests(output: String): Int {
        for (p in passedPatterns) {
            val m = p.matcher(output)
            if (m.find()) return m.group(1).toInt()
        }
        return 0
    }

}
