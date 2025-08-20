package core

import dto.*
import enums.ExecutionStatus
import enums.RequestStatus
import org.jetbrains.kotlin.cli.common.ExitCode
import org.jetbrains.kotlin.cli.jvm.K2JVMCompiler
import org.jetbrains.kotlin.cli.jvm.K2JVMCompilerArguments
import java.io.PrintWriter
import java.io.StringWriter
import java.io.File
import java.time.LocalDateTime
import java.util.concurrent.TimeUnit

class KotlinRunner {
    private val tmpDir = File("/tmp/kotlinc").apply { mkdirs() }

    fun wrapCode(solution: ProblemSolutionDto): String {
        val sb = StringBuilder()
        sb.appendLine("import org.junit.Test")
        sb.appendLine("import org.junit.Assert.*")
        sb.appendLine("import java.util.*")

        // добавляем дополнительные объявления (как в C#)
        for (def in solution.problem.additionalDefinitions) {
            sb.appendLine(def.value)
        }

        sb.appendLine(solution.code)

        sb.appendLine("class GeneratedTests {")

        for (test in solution.problem.testCases) {
            sb.appendLine("    @Test")
            sb.appendLine("    fun ${test.name}() {")
            if (test.testInitialization.isNotBlank()) {
                sb.appendLine("        ${test.testInitialization}")
            }
            sb.appendLine("        ${test.inputExpression}")
            sb.appendLine("        ${test.outputExpression}")
            sb.appendLine("    }")
        }

        sb.appendLine("}")
        return sb.toString()
    }

    fun compile(code: String): Pair<Boolean, String> {
        val srcFile = File(tmpDir, "UserProgram.kt")
        PrintWriter(srcFile).use { it.write(code) }

        val args = K2JVMCompilerArguments().apply {
            freeArgs = listOf(srcFile.absolutePath)
            destination = tmpDir.absolutePath
            includeRuntime = true
        }

        val compiler = K2JVMCompiler()
        val output = StringWriter()
        val exitCode: ExitCode = compiler.exec(PrintWriter(output), *org.jetbrains.kotlin.cli.common.arguments.parseCommandLineArguments(arrayOf()).toTypedArray())

        val success = exitCode == ExitCode.OK
        return success to output.toString()
    }

    fun execute(timeoutMs: Long = 5000): Pair<Int, String> {
        val process = ProcessBuilder(
            "java", "-cp", tmpDir.absolutePath, "org.junit.runner.JUnitCore", "GeneratedTests"
        ).redirectErrorStream(true).start()

        val finished = process.waitFor(timeoutMs, TimeUnit.MILLISECONDS)
        if (!finished) {
            process.destroyForcibly()
            return 124 to "Execution timed out."
        }

        val output = process.inputStream.bufferedReader().readText()
        return process.exitValue() to output
    }

    fun run(solution: ProblemSolutionDto): CodeResponseDto {
        val wrapped = wrapCode(solution)
        val (ok, compileMsg) = compile(wrapped)

        val now = LocalDateTime.now()

        return if (!ok) {
            CodeResponseDto(
                requestId = solution.requestId,
                requestStatus = RequestStatus.Failed,
                language = "kotlin",
                result = ExecutionResultDto(
                    status = ExecutionStatus.CompileError,
                    exitCode = 1,
                    consoleOutput = compileMsg,
                    requestSentAt = solution.sentAt,
                    responseSentAt = now
                )
            )
        } else {
            val (exitCode, output) = execute(solution.maxAllowedTimeInMilliseconds)

            val (status, reqStatus) = when {
                exitCode == 124 -> ExecutionStatus.TimedOut to RequestStatus.Failed
                exitCode == 0 -> ExecutionStatus.Succeded to RequestStatus.Succeeded
                else -> ExecutionStatus.RuntimeError to RequestStatus.Failed
            }

            CodeResponseDto(
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
}
