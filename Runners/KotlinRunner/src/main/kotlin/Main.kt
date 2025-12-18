import core.*
import dto.CodeResponseDto
import dto.ProblemSolutionDto
import core.JsonUtils
import org.slf4j.LoggerFactory

fun main(args: Array<String>) {
    val logger = LoggerFactory.getLogger("Main")
    val runner = KotlinRunner()

    when {
        args.contains("--once") -> {
            try {
                val input = System.`in`.readBytes().toString(Charsets.UTF_8)
                logger.info("[Main] Received input (${input.length} chars)")

                // Используем JsonUtils.objectMapper
                val request = JsonUtils.objectMapper.readValue(input,ProblemSolutionDto::class.java)
                logger.info("[Main] Parsed request: RequestId=${request.RequestId}, Manifest length=${request.TestManifestJson?.length}")

                val response: CodeResponseDto = runner.run(request, logger)

                val output = JsonUtils.objectMapper.writeValueAsString(response)
                println(output)
            } catch (e: Exception) {
                System.err.println("[KotlinRunner] CLI mode failed: $e")
                e.printStackTrace(System.err)
            }
        }

        args.contains("--server") -> {
            logger.info("[Main] Starting Kotlin runner server…")
            val server = RunnerServer(runner)
            server.start(port = 5000)
            logger.info("[Main] Server started on port 5000")
        }

        args.contains("--test") -> {
            // ... тестовый режим остается без изменений
        }

        else -> {
            System.err.println("Usage: java -jar app.jar [--once | --server | --test <json-file>]")
        }
    }
}