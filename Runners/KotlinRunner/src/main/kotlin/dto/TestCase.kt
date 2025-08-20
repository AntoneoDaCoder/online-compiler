package dto

data class TestCase(
    val name: String = "Test",
    val testLanguage: String = "",
    val testInitialization: String = "",
    val inputExpression: String = "",
    val outputExpression: String = ""
)