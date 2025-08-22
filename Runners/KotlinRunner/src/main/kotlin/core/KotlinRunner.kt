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
import java.util.*
import java.util.concurrent.*

class KotlinRunner {

    companion object {
        // Берём classpath текущего процесса (fat-jar Shadow включает всё):
        // stdlib + junit + hamcrest + сам раннер
        private val runtimeClasspath: String =
            System.getProperty("java.class.path") ?: ""

        // Базовая временная директория для компиляции
        private val tmpBaseDir = File(System.getProperty("java.io.tmpdir"), "kotlinc").apply { mkdirs() }
    }

    /**
     * Оборачиваем пользовательский код в junit-класс с автогенерируемыми тестами.
     */
    private fun wrapCode(solution: ProblemSolutionDto): String {
        val sb = StringBuilder()
        sb.appendLine("import org.junit.Test")
        sb.appendLine("import org.junit.Assert.*")
        sb.appendLine("import java.util.*")

        // дополнительные объявления (как у тебя в C#)
        solution.problem.additionalDefinitions.forEach { sb.appendLine(it.value) }

        // пользовательский код
        sb.appendLine(solution.code)

        // тесты
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

    /**
     * Компиляция в память: компилируем во временную папку, читаем .class в Map, папку удаляем.
     * Возвращаем (успех, картаКлассов, диагностическоеСообщениеКомпилятора)
     */
    private fun compileToMemory(code: String): Triple<Boolean, Map<String, ByteArray>, String> {
        val outDir = File(tmpBaseDir, UUID.randomUUID().toString()).apply { mkdirs() }
        val srcFile = File(outDir, "UserProgram.kt").apply { writeText(code) }

        // подавляем автодобавление stdlib/reflect, т.к. всё в нашем classpath
        val args = arrayOf(
            "-no-stdlib",
            "-no-reflect",
            "-jvm-target", "17",                 // совпадает с jvmToolchain
            "-classpath", runtimeClasspath,
            "-d", outDir.absolutePath,
            srcFile.absolutePath
        )

        val compiler = K2JVMCompiler()
        val baos = ByteArrayOutputStream()
        val ps = PrintStream(baos)

        val exit: ExitCode = compiler.exec(ps, *args)
        val diagnostics = baos.toString().trim()

        if (exit != ExitCode.OK) {
            outDir.deleteRecursively()
            return Triple(false, emptyMap(), diagnostics.ifEmpty { "Compilation failed" })
        }

        // Считываем байткод в память
        val byteMap = outDir.walkTopDown()
            .filter { it.isFile && it.extension == "class" }
            .associate { f ->
                val className = f.relativeTo(outDir).path.removeSuffix(".class").replace(File.separatorChar, '.')
                className to f.readBytes()
            }

        outDir.deleteRecursively()
        return Triple(true, byteMap, diagnostics)
    }

    /**
     * Кастомный ClassLoader, который подсовывает наш in-memory байткод.
     */
    private class InMemoryClassLoader(
        private val classes: Map<String, ByteArray>,
        parent: ClassLoader = ClassLoader.getSystemClassLoader()
    ) : ClassLoader(parent) {
        override fun findClass(name: String): Class<*> {
            val bytes = classes[name]
            if (bytes != null) return defineClass(name, bytes, 0, bytes.size)
            return super.findClass(name)
        }
    }

    /**
     * Запуск JUnit-тестов в отдельном потоке с таймаутом.
     * ВАЖНО: бесконечный цикл while(true) в том же процессе гарантированно не “убьёшь” прерыванием —
     * тут мы на таймаут возвращаем 124, но поток может продолжать жечь CPU до завершения request-scope executor.
     * Для полной гарантии килла — нужен изоляционный процесс (ProcessBuilder + kill).
     */
    private fun runTestsInMemory(classes: Map<String, ByteArray>, timeoutMs: Long): Pair<Int, String> {
        val loader = InMemoryClassLoader(classes)
        val executor = Executors.newSingleThreadExecutor { r ->
            Thread(r, "user-test-thread").apply { isDaemon = true }
        }
        val future = executor.submit(Callable {
            try {
                val testClass = loader.loadClass("GeneratedTests")
                val junitCore = org.junit.runner.JUnitCore()
                val result = junitCore.run(testClass)

                val output = buildString {
                    result.failures.forEach { appendLine(it.toString()) }
                    appendLine("Tests run: ${result.runCount}, Failures: ${result.failureCount}")
                }
                val exitCode = if (result.failureCount > 0) 1 else 0
                exitCode to output
            } catch (e: Throwable) {
                1 to e.stackTraceToString()
            }
        })

        return try {
            future.get(timeoutMs, TimeUnit.MILLISECONDS)
        } catch (e: TimeoutException) {
            future.cancel(true) // попытаемся прервать
            124 to "Execution timed out after ${timeoutMs}ms"
        } finally {
            executor.shutdownNow()
        }
    }

    /**
     * Внешний API раннера — аналог твоего C#.
     */
    fun run(solution: ProblemSolutionDto): CodeResponseDto {
        val now = LocalDateTime.now()
        val wrapped = wrapCode(solution)

        val (ok, classes, diag) = compileToMemory(wrapped)
        if (!ok) {
            return CodeResponseDto(
                requestId = solution.requestId,
                requestStatus = RequestStatus.Failed,
                language = "kotlin",
                result = ExecutionResultDto(
                    status = ExecutionStatus.CompileError,
                    exitCode = 1,
                    consoleOutput = diag,
                    requestSentAt = solution.sentAt,
                    responseSentAt = now
                )
            )
        }

        val (exitCode, output) = runTestsInMemory(classes, solution.maxAllowedTimeInMilliseconds)
        val (status, reqStatus) = when {
            exitCode == 124 -> ExecutionStatus.TimedOut to RequestStatus.Failed
            exitCode == 0   -> ExecutionStatus.Succeeded to RequestStatus.Succeeded
            else            -> ExecutionStatus.RuntimeError to RequestStatus.Failed
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
