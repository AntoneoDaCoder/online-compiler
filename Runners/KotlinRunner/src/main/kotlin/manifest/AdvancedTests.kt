package manifest

import com.fasterxml.jackson.annotation.JsonProperty

data class AdvancedTest(
    @field:JsonProperty("Name")
    var name: String? = null,
    @field:JsonProperty("TimeoutMs")
    var timeoutMs: Long = 0L,
    @field:JsonProperty("LanguageCode")
    var languagecode: String? = null,
    @field:JsonProperty("Source")
    var source: String? = null
)
