// TypeDescriptor.kt
package manifest

data class TypeDescriptor(
    // kind: "primitive","array","class","nullable","task" (if needed)
    var kind: String = "primitive",
    var name: String? = null, // primitive name or class FQN
    var items: TypeDescriptor? = null, // for arrays
    var of: TypeDescriptor? = null // for nullable / task.of
)
