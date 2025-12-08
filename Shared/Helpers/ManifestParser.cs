using ServerAPIApp.Domain.Exceptions.BadRequestExceptions;
using Shared.DTOs;
using Shared.DTOs.ManifestHelpers;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace Shared.Helpers
{
    public static class ManifestParser
    {
        private static readonly JsonSerializerOptions _opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
        };

        static ManifestParser()
        {
            _opts.ReadCommentHandling = JsonCommentHandling.Skip;
            _opts.NumberHandling = JsonNumberHandling.Strict;
            _opts.IgnoreReadOnlyProperties = false;
        }


        public static ManifestDto Parse(string json)
            => Parse(json, null);

        public static ManifestDto Parse(string json, string languageCode)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidTestTemplateException("Manifest is empty");

            ManifestDto manifest;
            try
            {
                manifest = JsonSerializer.Deserialize<ManifestDto>(json, _opts);
            }
            catch (Exception ex)
            {
                throw new InvalidTestTemplateException($"Failed to deserialize manifest: {ex.Message}");
            }

            if (manifest == null)
                throw new InvalidTestTemplateException("Failed to deserialize manifest (null)");

            if (string.IsNullOrWhiteSpace(manifest.Entrypoint))
                throw new InvalidTestTemplateException("Manifest.entrypoint is required");

            if (manifest.Signature == null)
                throw new InvalidTestTemplateException("Manifest.signature is required");

            var lang = string.IsNullOrWhiteSpace(languageCode) ? null : languageCode.Trim().ToLowerInvariant();

            if (!string.IsNullOrWhiteSpace(lang))
            {
                if (manifest.Helpers != null)
                {
                    manifest.Helpers = manifest.Helpers
                        .Where(h => !string.IsNullOrWhiteSpace(GetLanguageCode(h?.LanguageCode)))
                        .Where(h => string.Equals(GetLanguageCode(h.LanguageCode), lang, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
                else
                {
                    manifest.Helpers = new List<HelpersBlock>();
                }

                if (manifest.AdvancedTests != null)
                {
                    manifest.AdvancedTests = manifest.AdvancedTests
                        .Where(a => !string.IsNullOrWhiteSpace(GetLanguageCode(a?.LanguageCode)))
                        .Where(a => string.Equals(GetLanguageCode(a.LanguageCode), lang, StringComparison.OrdinalIgnoreCase))
                        .ToList();
                }
                else
                {
                    manifest.AdvancedTests = new List<AdvancedTest>();
                }
            }
            else
            {
                manifest.Helpers ??= new List<HelpersBlock>();
                manifest.AdvancedTests ??= new List<AdvancedTest>();
            }

            manifest.SampleTests ??= new List<SampleTest>();

            return manifest;
        }

        private static string GetLanguageCode(string code)
            => string.IsNullOrWhiteSpace(code) ? string.Empty : code.Trim().ToLowerInvariant();
    }
}
