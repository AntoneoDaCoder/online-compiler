package core

import com.fasterxml.jackson.databind.DeserializationFeature
import com.fasterxml.jackson.databind.MapperFeature
import com.fasterxml.jackson.datatype.jsr310.JavaTimeModule
import dto.CodeResponseDto
import dto.ProblemSolutionDto
import core.JsonUtils
import io.ktor.server.application.*
import io.ktor.server.engine.embeddedServer
import io.ktor.server.cio.CIO as ServerCIO
import io.ktor.server.plugins.contentnegotiation.ContentNegotiation as ServerContentNegotiation
import io.ktor.server.request.*
import io.ktor.server.response.*
import io.ktor.server.routing.*
import io.ktor.client.HttpClient
import io.ktor.client.engine.cio.CIO as ClientCIO
import io.ktor.client.plugins.contentnegotiation.ContentNegotiation as ClientContentNegotiation
import io.ktor.client.request.*
import io.ktor.client.statement.*
import io.ktor.http.*
import io.ktor.server.plugins.*
import kotlinx.coroutines.launch
import io.ktor.server.plugins.callloging.*
import io.ktor.serialization.jackson.jackson
import org.slf4j.event.Level
import io.ktor.server.plugins.statuspages.*


class RunnerServer(
    private val runner: KotlinRunner,
    private val callbackUrl: String =
        "http://api-server.default.svc.cluster.local:8080/api/jobs/complete"
) {

    private val objectMapper = JsonUtils.objectMapper

    private val client = HttpClient(ClientCIO) {
        install(ClientContentNegotiation) {
            jackson {
                // Копируем конфигурацию из JsonUtils
                registerModule(JavaTimeModule())
                configure(DeserializationFeature.FAIL_ON_UNKNOWN_PROPERTIES, false)
                configure(DeserializationFeature.READ_UNKNOWN_ENUM_VALUES_AS_NULL, true)
                disable(com.fasterxml.jackson.databind.SerializationFeature.WRITE_DATES_AS_TIMESTAMPS)
                    // Можно добавить дополнительные настройки для клиента
                }
            }

    }

        fun start(port: Int = 5000) {

            embeddedServer(ServerCIO, port) {
                install(ServerContentNegotiation) {
                    jackson {
                        // Копируем конфигурацию из JsonUtils
                        registerModule(JavaTimeModule())
                        configure(DeserializationFeature.FAIL_ON_UNKNOWN_PROPERTIES, false)
                        configure(DeserializationFeature.READ_UNKNOWN_ENUM_VALUES_AS_NULL, true)
                        disable(com.fasterxml.jackson.databind.SerializationFeature.WRITE_DATES_AS_TIMESTAMPS)
                    }
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
                        val raw = call.receiveText()
                        val request = objectMapper.readValue(raw, ProblemSolutionDto::class.java)
                        application.log.info("[Runner] Received request [Id:${request.RequestId}]")
                        launch {
                            val response = runner.run(request, application.log) // передаем json в раннер
                            notifyJobManager(response)
                        }

                        call.respondText("Accepted")
                    }
                }
            }.start(wait = true)
        }

    private suspend fun notifyJobManager(response: CodeResponseDto) {
        try {
            // НЕ вручную writeValueAsString(response)
            val httpResponse = client.post(callbackUrl) {
                contentType(ContentType.Application.Json)
                setBody(response) // client делает сериализацию один раз
            }

            val responseBody = httpResponse.bodyAsText()
            println("[Runner] Sent response [Id:${response.RequestId}], got HTTP ${httpResponse.status}")
            if (responseBody.isNotBlank()) println("[Runner] Response body: $responseBody")
        } catch (e: Exception) {
            println("[Runner] Failed to send response [Id:${response.RequestId}]: ${e.message}")
        }
    }

}
