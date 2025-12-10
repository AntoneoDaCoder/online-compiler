package dto

import com.fasterxml.jackson.annotation.JsonProperty
import enums.RequestStatus
import kotlinx.serialization.Contextual
import kotlinx.serialization.Serializable
import java.util.UUID

@Serializable
data class CodeResponseDto(
    @Contextual
    @JsonProperty("RequestId")
    var RequestId: UUID? = null,

    @JsonProperty("Status")
    var Status: RequestStatus? = null,

    @Contextual
    @JsonProperty("VersionId")
    var VersionId: UUID? = null,

    @Contextual
    @JsonProperty("UserId")
    var UserId: UUID? = null,

    @JsonProperty("UserSolution")
    var UserSolution: String? = null,

    @JsonProperty("Language")
    var Language: String? = null,

    @Contextual
    @JsonProperty("Result")
    var Result: ExecutionResultDto? = null
)
