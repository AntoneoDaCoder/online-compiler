package com.mems.helpers;

import com.fasterxml.jackson.core.JsonParser;
import com.fasterxml.jackson.databind.DeserializationFeature;
import com.fasterxml.jackson.databind.MapperFeature;
import com.fasterxml.jackson.databind.ObjectMapper;
import com.mems.manifest.*;
import java.util.ArrayList;
import java.util.List;
import java.util.Locale;
import java.util.stream.Collectors;

/**
 * Парсер манифеста: десериализует JSON в ManifestDto и фильтрует helpers/advancedTests по languageCode.
 */
public final class ManifestParser {

    private static final ObjectMapper MAPPER = createMapper();

    private static ObjectMapper createMapper() {
        ObjectMapper m = new ObjectMapper();
        // нечувствительность к регистру имён полей
        m.configure(MapperFeature.ACCEPT_CASE_INSENSITIVE_PROPERTIES, true);
        // позволить хвостовые запятые
        m.configure(JsonParser.Feature.ALLOW_TRAILING_COMMA, true);
        // не падать при неизвестных полях
        m.configure(DeserializationFeature.FAIL_ON_UNKNOWN_PROPERTIES, false);
        return m;
    }

    public static ManifestDto parse(String json, String languageCode) {
        if (json == null || json.trim().isEmpty()) {
            throw new IllegalArgumentException("Manifest is empty");
        }

        ManifestDto manifest;
        try {
            manifest = MAPPER.readValue(json, ManifestDto.class);
        } catch (Exception ex) {
            throw new IllegalArgumentException("Failed to deserialize manifest: " + ex.getMessage(), ex);
        }

        if (manifest == null) {
            throw new IllegalArgumentException("Failed to deserialize manifest (null)");
        }

        if (manifest.entrypoint == null || manifest.entrypoint.trim().isEmpty()) {
            throw new IllegalArgumentException("Manifest.entrypoint is required");
        }

        if (manifest.signature == null) {
            throw new IllegalArgumentException("Manifest.signature is required");
        }

        // Нормализованная кодировка языка для сравнения
        String lang = (languageCode == null) ? null : languageCode.trim().toLowerCase(Locale.ROOT);

        // Filter helpers: keep only those where languageCode matches exactly given language
        if (manifest.helpers != null && lang != null && !lang.isEmpty()) {
            List<HelpersBlock> filtered = manifest.helpers.stream()
                    .filter(h -> {
                        if (h == null) return false;
                        if (h.languageCode == null) return false;
                        return lang.equals(h.languageCode.trim().toLowerCase(Locale.ROOT));
                    })
                    .collect(Collectors.toList());
            manifest.helpers = filtered;
        } else if (manifest.helpers == null) {
            manifest.helpers = new ArrayList<>();
        }

        // Filter advanced tests by languageCode
        if (manifest.advancedTests != null && lang != null && !lang.isEmpty()) {
            List<AdvancedTest> filteredAdv = manifest.advancedTests.stream()
                    .filter(a -> {
                        if (a == null) return false;
                        if (a.languagecode == null) return false;
                        return lang.equals(a.languagecode.trim().toLowerCase(Locale.ROOT));
                    })
                    .collect(Collectors.toList());
            manifest.advancedTests = filteredAdv;
        } else if (manifest.advancedTests == null) {
            manifest.advancedTests = new ArrayList<>();
        }

        // sampleTests — оставляем как есть (обычно не фильтруются по языку)
        if (manifest.sampleTests == null) manifest.sampleTests = new ArrayList<>();

        return manifest;
    }
}
