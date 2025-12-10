// SampleTest.kt
package manifest

import com.fasterxml.jackson.databind.JsonNode

data class SampleTest(
    var name: String? = null,
    var inputs: JsonNode? = null,    // arbitrary JSON
    var expected: JsonNode? = null,  // arbitrary JSON
    var comparator: String? = null,  // eq, neq, seq_eq, seq_eq_sorted, contains, lt, gt, le, ge
    var timeoutMs: Long = 0L
)
