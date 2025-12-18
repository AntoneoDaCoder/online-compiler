// HelpersBlock.kt
package manifest

import com.fasterxml.jackson.annotation.JsonProperty

data class HelpersBlock(
    @field:JsonProperty("Inline")
    var inline: String? = null,
    @field:JsonProperty("LanguageCode")
    var languageCode: String? = null
)
