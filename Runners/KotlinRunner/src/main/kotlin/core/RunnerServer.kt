package core

import dto.CodeResponseDto
import dto.ProblemSolutionDto

// ---------- SERVER ----------
import io.ktor.server.application.*
import io.ktor.server.engine.embeddedServer
import io.ktor.server.cio.CIO as ServerCIO
import io.ktor.server.plugins.contentnegotiation.ContentNegotiation as ServerContentNegotiation
import io.ktor.server.request.*
import io.ktor.server.response.*
import io.ktor.server.routing.*
import io.ktor.serialization.kotlinx.json.json as serverJson

// ---------- CLIENT ----------
import io.ktor.client.HttpClient
import io.ktor.client.engine.cio.CIO as ClientCIO
import io.ktor.client.plugins.contentnegotiation.ContentNegotiation as ClientContentNegotiation
import io.ktor.serialization.kotlinx.json.json as clientJson
import io.ktor.client.request.*
import io.ktor.http.*

// ---------- COMMON ----------
import kotlinx.coroutines.launch
import kotlinx.serialization.json.Json

class RunnerServer(
    private val runner: KotlinRunner,
    private val callbackUrl: String =
        "http://api-server.default.svc.cluster.local:8080/api/jobs/complete"
) {
    private val client = HttpClient(ClientCIO) {
        install(ClientContentNegotiation) {
            clientJson(Json {
                ignoreUnknownKeys = true
                prettyPrint = false
                encodeDefaults = true
            })
        }
    }

    fun start(port: Int = 5000) {
        embeddedServer(ServerCIO, port) {
            install(ServerContentNegotiation) {
                serverJson()
            }
            routing {
                post("/run") {
                    val request = call.receive<ProblemSolutionDto>()
                    println("[Runner] Received request [Id:${request.requestId}]")

                    launch {
                        val response = runner.run(request)
                        notifyJobManager(response)
                    }

                    call.respondText("Accepted")
                }
            }
        }.start(wait = true)
    }

    private suspend fun notifyJobManager(response: CodeResponseDto) {
        try {
            val httpResponse = client.post(callbackUrl) {
                contentType(ContentType.Application.Json)
                setBody(response)
            }
            println("[Runner] Sent response [Id:${response.requestId}], got HTTP ${httpResponse.status}")
        } catch (e: Exception) {
            println("[Runner] Failed to send response [Id:${response.requestId}]: ${e.message}")
        }
    }
}
