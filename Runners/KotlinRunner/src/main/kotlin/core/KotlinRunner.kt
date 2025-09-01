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
    companion object { //JUnit4 runtime classpath
        private const val junitClasspath: String = "/libs/junit-4.13.2.jar:/libs/hamcrest-core-1.3.jar"

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

    fun run(solution: ProblemSolutionDto, logger: org.slf4j.Logger): CodeResponseDto {
        val now = LocalDateTime.now()
        logger.info("[Runner] Wrapping code for request [Id:${solution.requestId}]")
        val wrapped =
            wrapCode(solution)
        logger.info("[Runner] Compiling code for request [Id:${solution.requestId}]")
        val ok = compileToTmpDir(wrapped, logger)
        if (!ok) {
            logger.info("[Runner] Compilation failed for request [Id:${solution.requestId}]")
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
        logger.info("[Runner] Running tests for request [Id:${solution.requestId}]")
        val (exitCode, output) = runTestsInProcess(
            solution.maxAllowedTimeInMilliseconds,
            logger
        )
        val (status, reqStatus) = when {
            exitCode == 124 -> ExecutionStatus.TimedOut to RequestStatus.Failed
            exitCode ==
                    0 -> ExecutionStatus.Succeeded to RequestStatus.Succeeded

            else -> ExecutionStatus.RuntimeError to RequestStatus.Failed
        }
        logger.info("[Runner] Finished request [Id:${solution.requestId}] with status $status")
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

    private fun compileToTmpDir(code: String, logger: org.slf4j.Logger): Boolean {
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
        val exit = compiler.exec(
            ps,
            *args
        )
        if (exit != ExitCode.OK) logger.info("[Runner] Compiler output: ${baos.toString()}")
        return exit == ExitCode.OK
    }

    private fun runTestsInProcess(timeoutMs: Long, logger: org.slf4j.Logger): Pair<Int, String> {
        val globalTimeoutMs = 25_000L
        val classpath = "${tmpBaseDir.absolutePath}${File.pathSeparator}$runtimeClasspath"
        val cmd = listOf(
            System.getProperty("java.home") + File.separator + "bin" + File.separator + "java",
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
            val output = process.inputStream.bufferedReader()
                .readText()
            tmpBaseDir.listFiles()?.forEach { it.deleteRecursively() }
            process.exitValue() to output
        }
    }

    private fun wrapCode(solution: ProblemSolutionDto): String {
        val sb = StringBuilder()
        sb.appendLine("import org.junit.Test")
        sb.appendLine("import org.junit.Assert.*")
        sb.appendLine("import java.util.*")
        solution.problem.additionalDefinitions.forEach {
            sb.appendLine(it.value)
        }
        sb.appendLine(solution.code)
        sb.appendLine("class GeneratedTests {")

        solution.problem.testCases.forEach { test ->
            val timeoutMs =
                solution.maxAllowedTimeInMilliseconds
            sb.appendLine(" @Test(timeout = $timeoutMs)")
            sb.append(" fun ").append(
                test.name
            )
                .appendLine("() {")
            if (test.testInitialization.isNotBlank())
                sb.appendLine(" ${test.testInitialization}")
            sb.appendLine(" ${test.inputExpression}")
            sb.appendLine(" ${test.outputExpression}")
            sb.appendLine(" }")
        }
        sb.appendLine("}")
        return sb.toString()
    }
}