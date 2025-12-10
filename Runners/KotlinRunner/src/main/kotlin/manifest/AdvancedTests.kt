package manifest

data class AdvancedTest(
    var name: String? = null,
    var timeoutMs: Long = 0L,
    var languagecode: String? = null,
    var source: String? = null
)
