package dto

import enums.ExecutionStatus
import java.time.Duration
import java.time.LocalDateTime

data class ExecutionResultDto(
    val status: ExecutionStatus,
    val exitCode: Int,
    val consoleOutput: String?,
    val requestSentAt: LocalDateTime,
    val responseSentAt: LocalDateTime
) {
    val latencyInSeconds: Double
        get() = Duration.between(requestSentAt, responseSentAt).toMillis() / 1000.0
}