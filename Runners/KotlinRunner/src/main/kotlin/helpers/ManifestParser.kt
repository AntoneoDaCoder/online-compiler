package helpers

import com.fasterxml.jackson.core.JsonParser
import com.fasterxml.jackson.databind.DeserializationFeature
import com.fasterxml.jackson.databind.MapperFeature
import com.fasterxml.jackson.databind.ObjectMapper
import manifest.ManifestDto
import java.util.Locale

/**
 * Парсер манифеста: десериализует JSON в ManifestDto и фильтрует helpers/advancedTests по languageCode.
 */
object ManifestParser {

    private val MAPPER: ObjectMapper = createMapper()

    private fun createMapper(): ObjectMapper {
        val m = ObjectMapper()
        // нечувствительность к регистру имён полей
        m.configure(MapperFeature.ACCEPT_CASE_INSENSITIVE_PROPERTIES, true)
        // позволить хвостовые запятые
        m.configure(JsonParser.Feature.ALLOW_TRAILING_COMMA, true)
        // не падать при неизвестных полях
        m.configure(DeserializationFeature.FAIL_ON_UNKNOWN_PROPERTIES, false)
        return m
    }

    @JvmStatic
    fun parse(json: String?, languageCode: String?): ManifestDto {
        if (json == null || json.trim().isEmpty()) {
            throw IllegalArgumentException("Manifest is empty")
        }

        val manifest: ManifestDto = try {
            MAPPER.readValue(json, ManifestDto::class.java)
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
