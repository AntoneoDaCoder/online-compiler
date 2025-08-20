package dto

import java.time.LocalDateTime
import java.util.UUID

data class ProblemSolutionDto(
    val requestId: UUID,
    val maxAllowedTimeInMilliseconds: Long,
    val language: String = "",
    val code: String = "",
    val callbackUrl: String = "",
    val problem: Problem,
    val sentAt: LocalDateTime
)