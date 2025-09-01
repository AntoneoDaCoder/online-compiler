package dto
import kotlinx.serialization.Serializable

@Serializable
data class AdditionalDefinition(
    val language: String = "",
    val value: String = ""
)