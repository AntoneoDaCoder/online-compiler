package dto

import com.fasterxml.jackson.annotation.JsonFormat
import com.fasterxml.jackson.annotation.JsonProperty
import enums.ExecutionStatus
import kotlinx.serialization.Contextual
import kotlinx.serialization.Serializable
import java.time.OffsetDateTime

@Serializable
data class ExecutionResultDto(
    @JsonProperty("Status")
    var Status: ExecutionStatus? = null,

    @JsonProperty("ExitCode")
    var ExitCode: Int = 0,

    @JsonProperty("ConsoleOutput")
    var ConsoleOutput: String? = null,

    @JsonProperty("PassedTests")
    var PassedTests: Int = 0,

    @JsonProperty("TotalTests")
    var TotalTests: Int = 0,

    @JsonProperty("RequestSentAt")
    @Contextual
    @JsonFormat(shape = JsonFormat.Shape.STRING)
    var RequestSentAt: OffsetDateTime? = null,

    @JsonProperty("ResponseSentAt")
    @Contextual
    @JsonFormat(shape = JsonFormat.Shape.STRING)
    var ResponseSentAt: OffsetDateTime? = null
)