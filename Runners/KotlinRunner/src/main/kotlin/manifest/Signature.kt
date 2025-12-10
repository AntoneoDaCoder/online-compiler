// Signature.kt
package manifest

data class Signature(
    var returnType: TypeDescriptor = TypeDescriptor(),
    var parameters: MutableList<ParameterDescriptor> = mutableListOf()
)
