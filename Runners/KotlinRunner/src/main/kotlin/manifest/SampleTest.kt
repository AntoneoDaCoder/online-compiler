// SampleTest.kt
package manifest

import com.fasterxml.jackson.annotation.JsonProperty
import com.fasterxml.jackson.databind.JsonNode

data class SampleTest(
    @field:JsonProperty("Name")
    var name: String? = null,
    @field:JsonProperty("Inputs")
    var inputs: JsonNode? = null,    // arbitrary JSON
    @field:JsonProperty("Expected")
    var expected: JsonNode? = null,  // arbitrary JSON
    @field:JsonProperty("Comparator")
    var comparator: String? = null,  // eq, neq, seq_eq, seq_eq_sorted, contains, lt, gt, le, ge
    @field:JsonProperty("TimeoutMs")
    var timeoutMs: Long = 0L
)
