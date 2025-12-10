package manifest

data class ManifestDto(
    var entrypoint: String? = null,
    var signature: Signature = Signature(),
    var helpers: List<HelpersBlock>? = null,
    var advancedTests: List<AdvancedTest>? = null,
    var sampleTests: List<SampleTest>? = null
)
