package dto

import com.fasterxml.jackson.annotation.JsonFormat
import com.fasterxml.jackson.annotation.JsonProperty
import kotlinx.serialization.Contextual
import kotlinx.serialization.Serializable
import java.time.OffsetDateTime

@Serializable
data class ProblemSolutionDto(
    @JsonProperty("RequestId")
    var RequestId: String? = null,

    @JsonProperty("VersionId")
    var VersionId: String? = null,

    @JsonProperty("UserId")
    var UserId: String? = null,

    @JsonProperty("TestManifestJson")
    var TestManifestJson: String? = null,

    @JsonProperty("LanguageCode")
    var LanguageCode: String? = null,

    @JsonProperty("UserSolution")
    var UserSolution: String? = null,

    @Contextual
    @JsonProperty("SentAt")
    @JsonFormat(shape = JsonFormat.Shape.STRING)
    var SentAt: OffsetDateTime? = null
)