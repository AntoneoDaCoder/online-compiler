import core.KotlinRunner
import core.RunnerServer
import dto.CodeResponseDto
import dto.ProblemSolutionDto
import kotlinx.serialization.json.Json
import kotlinx.serialization.modules.SerializersModule
import org.slf4j.LoggerFactory
import java.time.LocalDateTime
import java.util.UUID
import core.UUIDSerializer
import core.LocalDateTimeSerializer

fun main(args: Array<String>) {
    val logger = LoggerFactory.getLogger("Main")
    val runner = KotlinRunner()

    // тот же Json, что и в RunnerServer
    val json = Json {
        ignoreUnknownKeys = true
        prettyPrint = false
        encodeDefaults = true
        serializersModule = SerializersModule {
            contextual(UUID::class, UUIDSerializer)
            contextual(LocalDateTime::class, LocalDateTimeSerializer)
        }
    }

    when {
        args.contains("--once") -> {
            try {
                val input = System.`in`.readBytes().toString(Charsets.UTF_8)
                val request = json.decodeFromString(ProblemSolutionDto.serializer(), input)

                val response: CodeResponseDto = runner.run(request, logger)

                println(json.encodeToString(CodeResponseDto.serializer(), response))
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

        else -> {
            System.err.println("Usage: java -jar app.jar [--once | --server]")
        }
    }
}
