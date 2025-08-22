package dto

import enums.RequestStatus
import java.util.UUID
import kotlinx.serialization.Serializable
import kotlinx.serialization.Contextual

@Serializable
data class CodeResponseDto(
    @Contextual
    val requestId: UUID,
    val requestStatus: RequestStatus,
    val language: String = "",
    val result: ExecutionResultDto
)