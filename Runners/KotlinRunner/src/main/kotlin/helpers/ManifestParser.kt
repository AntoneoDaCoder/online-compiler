package helpers

import core.JsonUtils
import manifest.ManifestDto
import java.util.Locale

/**
 * Парсер манифеста: десериализует JSON в ManifestDto и фильтрует helpers/advancedTests по languageCode.
 */
object ManifestParser {
    @JvmStatic
    fun parse(json: String?, languageCode: String?): ManifestDto {
        if (json == null || json.trim().isEmpty()) {
            throw IllegalArgumentException("Manifest is empty")
        }

        val manifest: ManifestDto = try {
            JsonUtils.objectMapper.readValue<ManifestDto>(json, ManifestDto::class.java)
        } catch (ex: Exception) {
            throw IllegalArgumentException("Failed to deserialize manifest: ${ex.message}", ex)
        } ?: throw IllegalArgumentException("Failed to deserialize manifest (null)")

        if (manifest.entrypoint == null || manifest.entrypoint!!.trim().isEmpty()) {
            throw IllegalArgumentException("Manifest.entrypoint is required")
        }

        if (manifest.signature == null) {
            throw IllegalArgumentException("Manifest.signature is required")
        }

        val lang = languageCode?.trim()?.lowercase(Locale.ROOT)

        // Filter helpers: keep only those where languageCode matches exactly given language
        if (!manifest.helpers.isNullOrEmpty() && !lang.isNullOrEmpty()) {
            val filtered = manifest.helpers!!.filter { h ->
                !h.languageCode.isNullOrEmpty() &&
                        lang == h.languageCode!!.trim().lowercase(Locale.ROOT)
            }
            manifest.helpers = filtered
        } else if (manifest.helpers == null) {
            manifest.helpers = emptyList()
        }

        // Filter advanced tests by languageCode
        if (!manifest.advancedTests.isNullOrEmpty() && !lang.isNullOrEmpty()) {
            val filteredAdv = manifest.advancedTests!!.filter { a ->
                !a.languagecode.isNullOrEmpty() &&
                        lang == a.languagecode!!.trim().lowercase(Locale.ROOT)
            }
            manifest.advancedTests = filteredAdv
        } else if (manifest.advancedTests == null) {
            manifest.advancedTests = emptyList()
        }

        // sampleTests — оставляем как есть (обычно не фильтруются по языку)
        if (manifest.sampleTests == null) manifest.sampleTests = emptyList()

        return manifest
    }
}
