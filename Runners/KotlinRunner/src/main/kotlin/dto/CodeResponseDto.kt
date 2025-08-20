package dto

import enums.RequestStatus
import java.util.UUID

data class CodeResponseDto(
    val requestId: UUID,
    val requestStatus: RequestStatus,
    val language: String = "",
    val result: ExecutionResultDto
)