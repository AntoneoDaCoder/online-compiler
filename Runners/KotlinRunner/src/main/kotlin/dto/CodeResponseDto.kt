package dto

import com.fasterxml.jackson.annotation.JsonProperty
import enums.RequestStatus
import java.util.UUID

data class CodeResponseDto(
    @field:JsonProperty("RequestId")
    var RequestId: UUID? = null,

    @field:JsonProperty("Status")
    var Status: RequestStatus? = null,

    @field:JsonProperty("VersionId")
    var VersionId: UUID? = null,

    @field:JsonProperty("UserId")
    var UserId: UUID? = null,

    @field:JsonProperty("UserSolution")
    var UserSolution: String? = null,

    @field:JsonProperty("Language")
    var Language: String? = null,

    @field:JsonProperty("Result")
    var Result: ExecutionResultDto? = null
)
