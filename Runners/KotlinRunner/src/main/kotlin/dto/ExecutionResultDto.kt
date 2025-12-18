package dto

import com.fasterxml.jackson.annotation.JsonFormat
import com.fasterxml.jackson.annotation.JsonProperty
import enums.ExecutionStatus
import java.time.OffsetDateTime

data class ExecutionResultDto(
    @field:JsonProperty("Status")
    var Status: ExecutionStatus? = null,

    @field:JsonProperty("ExitCode")
    var ExitCode: Int = 0,

    @field:JsonProperty("ConsoleOutput")
    var ConsoleOutput: String? = null,

    @field:JsonProperty("PassedTests")
    var PassedTests: Int = 0,

    @field:JsonProperty("TotalTests")
    var TotalTests: Int = 0,

    @field:JsonProperty("RequestSentAt")
    @field:JsonFormat(shape = JsonFormat.Shape.STRING)
    var RequestSentAt: OffsetDateTime? = null,

    @field:JsonProperty("ResponseSentAt")
    @field:JsonFormat(shape = JsonFormat.Shape.STRING)
    var ResponseSentAt: OffsetDateTime? = null
)