package dto
import kotlinx.serialization.Serializable

@Serializable
data class TestCase(
    val name: String = "Test",
    val testLanguage: String = "",
    val testInitialization: String = "",
    val inputExpression: String = "",
    val outputExpression: String = ""
)