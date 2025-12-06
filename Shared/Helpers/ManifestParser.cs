using System.Text.Json;
using Shared.DTOs;
using ServerAPIApp.Domain.Exceptions.BadRequestExceptions;

namespace Shared.Helpers
{
    public static class ManifestParser
    {
        private static readonly JsonSerializerOptions _opts = new JsonSerializerOptions
        {
            PropertyNameCaseInsensitive = true,
            AllowTrailingCommas = true,
        };

        public static ManifestDto Parse(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidTestTemplateException("Manifest is empty");

            var manifest = JsonSerializer.Deserialize<ManifestDto>(json, _opts)
                ?? throw new InvalidTestTemplateException("Failed to deserialize manifest");

            if (string.IsNullOrWhiteSpace(manifest.Entrypoint))
                throw new InvalidTestTemplateException("Manifest.entrypoint is required");

            if (manifest.Signature == null)
                throw new InvalidTestTemplateException("Manifest.signature is required");

            return manifest;
        }
    }
}
