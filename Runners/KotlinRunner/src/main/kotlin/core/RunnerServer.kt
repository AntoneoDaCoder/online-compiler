package core

import dto.CodeResponseDto
import dto.ProblemSolutionDto
import io.ktor.server.application.*
import io.ktor.server.engine.embeddedServer
import io.ktor.server.cio.CIO as ServerCIO
import io.ktor.server.plugins.contentnegotiation.ContentNegotiation as ServerContentNegotiation
import io.ktor.server.request.*
import io.ktor.server.response.*
import io.ktor.server.routing.*
import io.ktor.serialization.kotlinx.json.json as serverJson
import io.ktor.client.HttpClient
import io.ktor.client.engine.cio.CIO as ClientCIO
import io.ktor.client.plugins.contentnegotiation.ContentNegotiation as ClientContentNegotiation
import io.ktor.serialization.kotlinx.json.json as clientJson
import io.ktor.client.request.*
import io.ktor.client.statement.*
import io.ktor.http.*
import io.ktor.server.plugins.*
import kotlinx.coroutines.launch
import kotlinx.serialization.json.Json
import kotlinx.serialization.modules.SerializersModule
import io.ktor.server.plugins.callloging.*
import org.slf4j.event.Level
import java.util.UUID
import java.time.OffsetDateTime
import io.ktor.server.plugins.statuspages.*     // для StatusPages



class RunnerServer(
    private val runner: KotlinRunner,
    private val callbackUrl: String =
        "http://api-server.default.svc.cluster.local:8080/api/jobs/complete"
) {
    private val json = Json {
        ignoreUnknownKeys = true
        prettyPrint = false
        encodeDefaults = true
        serializersModule = SerializersModule {
            contextual(UUID::class, UUIDSerializer)
            contextual(OffsetDateTime::class, OffsetDateTimeSerializer)
        }
    }

    private val client = HttpClient(ClientCIO) {
        install(ClientContentNegotiation) {
            clientJson(json) // используем кастомный json
        }
    }

    fun start(port: Int = 5000) {

        embeddedServer(ServerCIO, port) {
            install(ServerContentNegotiation) {
                serverJson(json) // тоже используем кастомный json
            }
            install(StatusPages) {
                exception<Throwable> { call, cause ->
                    call.application.log.error("Unhandled exception for request ${call.request.uri}", cause)
                    call.respondText("Internal Server Error", status = HttpStatusCode.InternalServerError)
                }
            }

            install(CallLogging) {
                level = Level.INFO
                format { call ->
                    val req = call.request
                    "HTTP ${req.httpMethod.value} - ${req.uri} from ${req.origin.remoteHost}"
                }
            }

            routing {
                post("/run") {
                    val request = call.receive<ProblemSolutionDto>()
                    application.log.info("[Runner] Received request [Id:${request.RequestId}]")
                    launch {
                        val response = runner.run(request, application.log) // передаем json в раннер
                        val jsonString = json.encodeToString(CodeResponseDto.serializer(), response)
                        println(jsonString)
                        notifyJobManager(response)
                    }

                    call.respondText("Accepted")
                }
            }
        }.start(wait = true)
    }

    private suspend fun notifyJobManager(response: CodeResponseDto) {
        try {
            val jsonString = json.encodeToString(CodeResponseDto.serializer(), response)
            val httpResponse = client.post(callbackUrl) {
                contentType(ContentType.Application.Json)
                setBody(jsonString)
            }

            // читаем тело ответа как текст
            val responseBody = httpResponse.bodyAsText()

            println("[Runner] Sent response [Id:${response.RequestId}], got HTTP ${httpResponse.status}")
            if (responseBody.isNotBlank()) {
                println("[Runner] Response body: $responseBody")
            }
        } catch (e: Exception) {
            println("[Runner] Failed to send response [Id:${response.RequestId}]: ${e.message}")
        }
    }


}
