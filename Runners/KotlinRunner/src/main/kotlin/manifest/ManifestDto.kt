package manifest

import com.fasterxml.jackson.annotation.JsonProperty

data class ManifestDto(
    @field:JsonProperty("Entrypoint")
    var entrypoint: String? = null,
    @field:JsonProperty("Signature")
    var signature: Signature = Signature(),
    @field:JsonProperty("Helpers")
    var helpers: List<HelpersBlock>? = null,
    @field:JsonProperty("AdvancedTests")
    var advancedTests: List<AdvancedTest>? = null,
    @field:JsonProperty("SampleTests")
    var sampleTests: List<SampleTest>? = null
)
