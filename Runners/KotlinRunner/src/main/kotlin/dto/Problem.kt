package dto
import kotlinx.serialization.Serializable

@Serializable
data class Problem(
    val name: String = "",
    val additionalDefinitions: MutableList<AdditionalDefinition> = mutableListOf(),
    val testCases: MutableList<TestCase> = mutableListOf()
)