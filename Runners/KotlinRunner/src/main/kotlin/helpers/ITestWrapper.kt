package helpers

import manifest.ManifestDto

interface ITestWrapper {

    @Throws(Exception::class)
    fun generateSource(
        manifest: ManifestDto,
        languageCodeIn: String?,
        userCode: String?,
        entrypointContainerClassIn: String?,
        defaultTimeoutMs: Long
    ): String
}
