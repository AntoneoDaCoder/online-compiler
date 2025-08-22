package dto

import java.time.LocalDateTime
import java.util.UUID
import kotlinx.serialization.Serializable
import kotlinx.serialization.Contextual

@Serializable
data class ProblemSolutionDto(
    @Contextual
    val requestId: UUID,
    val maxAllowedTimeInMilliseconds: Long,
    val language: String = "",
    val code: String = "",
    val callbackUrl: String = "",
    val problem: Problem,
    @Contextual
    val sentAt: LocalDateTime
)