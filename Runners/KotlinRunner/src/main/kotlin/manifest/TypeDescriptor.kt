// TypeDescriptor.kt
package manifest

import com.fasterxml.jackson.annotation.JsonProperty

data class TypeDescriptor(
    // kind: "primitive","array","class","nullable","task" (if needed)
    @field:JsonProperty("Kind")
    var kind: String = "primitive",
    @field:JsonProperty("Name")
    var name: String? = null, // primitive name or class FQN
    @field:JsonProperty("Items")
    var items: TypeDescriptor? = null, // for arrays
    @field:JsonProperty("Of")
    var of: TypeDescriptor? = null // for nullable / task.of
)
