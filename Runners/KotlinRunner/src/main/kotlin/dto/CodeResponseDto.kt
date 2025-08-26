package dto

import enums.RequestStatus
import java.util.UUID
import kotlinx.serialization.Serializable
import kotlinx.serialization.SerialName
import kotlinx.serialization.Contextual

@Serializable
data class CodeResponseDto(
    @Contextual
    @SerialName("RequestId")
    val requestId: UUID,
    @SerialName("Status")
    val requestStatus: RequestStatus,
    @SerialName("Language")
    val language: String = "",
    @SerialName("Result")
    val result: ExecutionResultDto
)