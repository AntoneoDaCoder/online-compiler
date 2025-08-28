package dto

import enums.ExecutionStatus
import java.time.Duration
import java.time.LocalDateTime
import kotlinx.serialization.SerialName
import kotlinx.serialization.Serializable
import kotlinx.serialization.Contextual

@Serializable
data class ExecutionResultDto(
    @SerialName("Status")
    val status: ExecutionStatus,
    @SerialName("ExitCode")
    val exitCode: Int = 0,
    @SerialName("ConsoleOutput")
    val consoleOutput: String = "",
    @Contextual
    @SerialName("RequestSentAt")
    val requestSentAt: java.time.LocalDateTime,
    @Contextual
    @SerialName("ResponseSentAt")
    val responseSentAt: java.time.LocalDateTime
)