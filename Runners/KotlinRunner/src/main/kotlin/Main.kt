import core.*
import dto.CodeResponseDto
import dto.ProblemSolutionDto
import kotlinx.serialization.json.Json
import kotlinx.serialization.modules.SerializersModule
import org.slf4j.LoggerFactory
import java.util.UUID
import java.time.OffsetDateTime

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
            contextual(OffsetDateTime::class, OffsetDateTimeSerializer)
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

        args.contains("--test")->{

            val userCode = """
                fun Solution(mode: String?, arr: IntArray?): IntArray {
                    if (arr == null) return IntArray(0)
                    val m = mode ?: ""

                    return when (m) {
                        "identity" -> arr.copyOf()
                        "sort" -> {
                            val copy = arr.copyOf()
                            java.util.Arrays.sort(copy)
                            copy
                        }
                        "sleep" -> {
                            try {
                                Thread.sleep(5000)
                            } catch (ie: InterruptedException) {
                                // ignore
                            }
                            arr.copyOf()
                        }
                        "sum_as_array" -> {
                            var s = 0
                            for (x in arr) s += x
                            intArrayOf(s)
                        }
                        else -> arr.copyOf()
                    }
                }
            """.trimIndent()

            val jsonPath = args.getOrNull(args.indexOf("--test") + 1)
                ?: throw IllegalArgumentException("Expected path to request JSON after --test")

            try {
                println("[KotlinRunner] Local test mode. Reading manifest/request: $jsonPath")

                val jsonText = java.nio.file.Files.readString(java.nio.file.Paths.get(jsonPath), java.nio.charset.StandardCharsets.UTF_8)

                // parse manifest (filtering by kotlin)
                val manifest = helpers.ManifestParser.parse(jsonText, "kotlin")

                // userCode declared above in this block
                val entrypointClass = "SolutionContainer"
                val defaultTimeoutMs = 2000L

                // generate full source using KotlinWrapper (must implement ITestWrapper.generateSource)
                val wrapper = helpers.KotlinWrapper
                val fullSource = wrapper.generateSource(manifest, "kotlin", userCode, entrypointClass, defaultTimeoutMs)

                // write GeneratedTests.kt to same directory as manifest file (or current dir if not available)
                val jsonFile = java.nio.file.Paths.get(jsonPath)
                val outDir = if (jsonFile.parent != null) jsonFile.parent else java.nio.file.Paths.get(".")
                val outPath = outDir.resolve("GeneratedTests.kt")
                java.nio.file.Files.writeString(outPath, fullSource, java.nio.charset.StandardCharsets.UTF_8)

                println("[KotlinRunner] Generated source written to: ${outPath.toAbsolutePath()}")
                println()
                println("[KotlinRunner] How to compile and run the generated tests locally (example):")
                println("  1) obtain junit and hamcrest jars, e.g.: junit-4.13.2.jar and hamcrest-core-1.3.jar")
                println("  2) compile with kotlinc (adjust paths):")
                println("     kotlinc ${outPath.toAbsolutePath()} -d generated_classes -classpath /path/to/junit-4.13.2.jar:/path/to/hamcrest-core-1.3.jar")
                println("     (on Windows use ';' instead of ':')")
                println("  3) make sure kotlin stdlib is available on the classpath (kotlinc usually bundles it; if not, add kotlin-stdlib.jar):")
                println("     java -cp generated_classes:/path/to/junit-4.13.2.jar:/path/to/hamcrest-core-1.3.jar:/path/to/kotlin-stdlib.jar org.junit.runner.JUnitCore GeneratedTests")
                println("     (on Windows use ';' classpath separator)")
                println()
                println("Notes:")
                println(" - Replace /path/to/... with actual paths to the jars on your machine.")
                println(" - If kotlinc produces a jar instead of class files use that jar on the classpath instead of generated_classes.")
                println(" - In IDE: create a Kotlin project, add junit & hamcrest libs, put GeneratedTests.kt under sources and run JUnit runner for GeneratedTests.")
            } catch (ex: Exception) {
                System.err.println("[KotlinRunner] runLocalTestFile failed: $ex")
                ex.printStackTrace()
            }
        }

        else -> {
            System.err.println("Usage: java -jar app.jar [--once | --server]")
        }
    }
}
