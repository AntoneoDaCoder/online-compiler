import core.KotlinRunner
import dto.*
import java.time.LocalDateTime
import java.util.UUID

fun main() {
    val runner = KotlinRunner()

    val problem = Problem(
        name = "Demo",
        additionalDefinitions = mutableListOf(), // сюда можно подмешивать доп. классы/функции
        testCases = mutableListOf(
            TestCase(
                name = "testSum",
                testInitialization = "",
                inputExpression = "val result = 2 + 2",
                outputExpression = "assertEquals(4, result)"
            )
        )
    )

    val dto = ProblemSolutionDto(
        requestId = UUID.randomUUID(),
        maxAllowedTimeInMilliseconds = 3000,
        language = "kotlin",
        code =
        """
            // user code (можно оставить пустым, если тесты сами всё делают)
            """.trimIndent(),
        callbackUrl = "",
        problem = problem,
        sentAt = LocalDateTime.now()
    )

    val res = runner.run(dto)
    println("Status: ${res.result.status}")
    println("ExitCode: ${res.result.exitCode}")
    println("Console:\n${res.result.consoleOutput}")
}
