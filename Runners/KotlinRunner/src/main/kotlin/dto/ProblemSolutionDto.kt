package dto

import com.fasterxml.jackson.annotation.JsonFormat
import com.fasterxml.jackson.annotation.JsonProperty
import java.time.OffsetDateTime
import java.util.UUID

data class ProblemSolutionDto(
    @field:JsonProperty("requestId")
    var RequestId: UUID? = null,

    @field:JsonProperty("versionId")
    var VersionId: UUID? = null,

    @field:JsonProperty("userId")
    var UserId: UUID? = null,

    @field:JsonProperty("testManifestJson")
    var TestManifestJson: String? = null,

    @field:JsonProperty("languageCode")
    var LanguageCode: String? = null,

    @field:JsonProperty("userSolution")
    var UserSolution: String? = null,

    @field:JsonProperty("sentAt")
    @field:JsonFormat(shape = JsonFormat.Shape.STRING)
    var SentAt: OffsetDateTime? = null
)