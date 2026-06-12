// ParameterDescriptor.kt
package manifest

import com.fasterxml.jackson.annotation.JsonProperty

data class ParameterDescriptor(
    @field:JsonProperty("Name")
    var name: String = "",
    @field:JsonProperty("Type")
    var type: TypeDescriptor = TypeDescriptor()
)
