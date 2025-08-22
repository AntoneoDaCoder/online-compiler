package dto

import enums.ExecutionStatus
import java.time.Duration
import java.time.LocalDateTime
import kotlinx.serialization.Serializable
import kotlinx.serialization.Contextual

@Serializable
data class ExecutionResultDto(
    val status: ExecutionStatus,
    val exitCode: Int,
    val consoleOutput: String?,
    @Contextual
    val requestSentAt: LocalDateTime,
    @Contextual
    val responseSentAt: LocalDateTime
) {
    val latencyInSeconds: Double
        get() = Duration.between(requestSentAt, responseSentAt).toMillis() / 1000.0
}