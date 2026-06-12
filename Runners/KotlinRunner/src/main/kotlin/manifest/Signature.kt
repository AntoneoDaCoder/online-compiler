// Signature.kt
package manifest

import com.fasterxml.jackson.annotation.JsonProperty

data class Signature(
    @field:JsonProperty("ReturnType")
    var returnType: TypeDescriptor = TypeDescriptor(),
    @field:JsonProperty("Parameters")
    var parameters: MutableList<ParameterDescriptor> = mutableListOf()
)
