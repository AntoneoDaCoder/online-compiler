package core

import dto.*
import enums.ExecutionStatus
import enums.RequestStatus
import org.jetbrains.kotlin.cli.common.ExitCode
import org.jetbrains.kotlin.cli.jvm.K2JVMCompiler
import java.io.ByteArrayOutputStream
import java.io.File
import java.io.PrintStream
import java.time.LocalDateTime
import java.util.concurrent.TimeUnit

class KotlinRunner {

    companion object {
        private val runtimeClasspath: String = System.getProperty("java.class.path") ?: ""

        private val tmpBaseDir: File = File(System.getProperty("java.io.tmpdir"), "kotlinc").apply {
            mkdirs()
        }

        private fun cleanTmpDir() {
            tmpBaseDir.listFiles()?.forEach { it.deleteRecursively() }
        }
    }

    private fun wrapCode(solution: ProblemSolutionDto): String {
        val sb = StringBuilder()
        sb.appendLine("import org.junit.Test")
        sb.appendLine("import org.junit.Assert.*")
        sb.appendLine("import java.util.*")
        solution.problem.additionalDefinitions.forEach { sb.appendLine(it.value) }
        sb.appendLine(solution.code)
        sb.appendLine("class GeneratedTests {")
        solution.problem.testCases.forEach { test ->
            sb.append("  @Test fun ").append(test.name).appendLine("() {")
            if (test.testInitialization.isNotBlank()) sb.appendLine("    ${test.testInitialization}")
            sb.appendLine("    ${test.inputExpression}")
            sb.appendLine("    ${test.outputExpression}")
            sb.appendLine("  }")
        }
        sb.appendLine("}")
        return sb.toString()
    }

    private fun compileToTmpDir(code: String): Boolean {
        cleanTmpDir()
        val srcFile = File(tmpBaseDir, "UserProgram.kt").apply { writeText(code) }

        val args = arrayOf(
            "-no-stdlib",
            "-no-reflect",
            "-jvm-target", "17",
            "-classpath", runtimeClasspath,
            "-d", tmpBaseDir.absolutePath,
            srcFile.absolutePath
        )

        val compiler = K2JVMCompiler()
        val baos = ByteArrayOutputStream()
        val ps = PrintStream(baos)
        val exit = compiler.exec(ps, *args)
        return exit == ExitCode.OK
    }

    private fun runTestsInProcess(timeoutMs: Long): Pair<Int, String> {
        val classpath = "${tmpBaseDir.absolutePath}${File.pathSeparator}$runtimeClasspath"
        val cmd = listOf(
            System.getProperty("java.home") + File.separator + "bin" + File.separator + "java",
            "-cp", classpath,
            "org.junit.runner.JUnitCore",
            "GeneratedTests"
        )

        val processBuilder = ProcessBuilder(cmd)
            .redirectErrorStream(true)
        val process = processBuilder.start()
        val finished = process.waitFor(timeoutMs, TimeUnit.MILLISECONDS)

        return if (!finished) {
            process.destroyForcibly()
            124 to "Execution timed out after ${timeoutMs}ms"
        } else {
            val output = process.inputStream.bufferedReader().readText()

            tmpBaseDir.listFiles()?.forEach { it.deleteRecursively() }
            val exitCode = process.exitValue()
            exitCode to output
        }
    }

    fun run(solution: ProblemSolutionDto): CodeResponseDto {
        val now = LocalDateTime.now()
        val wrapped = wrapCode(solution)
        val ok = compileToTmpDir(wrapped)

        if (!ok) {
            return CodeResponseDto(
                requestId = solution.requestId,
                requestStatus = RequestStatus.Failed,
                language = "kotlin",
                result = ExecutionResultDto(
                    status = ExecutionStatus.CompileError,
                    exitCode = 1,
                    consoleOutput = "Compilation failed",
                    requestSentAt = solution.sentAt,
                    responseSentAt = now
                )
            )
        }

        val (exitCode, output) = runTestsInProcess(solution.maxAllowedTimeInMilliseconds)
        val (status, reqStatus) = when {
            exitCode == 124 -> ExecutionStatus.TimedOut to RequestStatus.Failed
            exitCode == 0 -> ExecutionStatus.Succeeded to RequestStatus.Succeeded
            else -> ExecutionStatus.RuntimeError to RequestStatus.Failed
        }

        return CodeResponseDto(
            requestId = solution.requestId,
            requestStatus = reqStatus,
            language = "kotlin",
            result = ExecutionResultDto(
                status = status,
                exitCode = exitCode,
                consoleOutput = output,
                requestSentAt = solution.sentAt,
                responseSentAt = now
            )
        )
    }
}
