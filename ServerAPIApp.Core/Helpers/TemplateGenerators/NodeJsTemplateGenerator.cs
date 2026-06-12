using ServerAPIApp.Core.Abstractions;
using Shared.DTOs;
using Shared.DTOs.ManifestHelpers;
using System.Text;

namespace ServerAPIApp.Core.Helpers.TemplateGenerators
{
    public class NodeJsTemplateGenerator : ITemplateGenerator
    {
        public string LanguageCode => "nodejs";

        public string BuildTemplate(ManifestDto manifest)
        {
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));

            var signature = manifest.Signature;
            var parameters = signature?.Parameters ?? new List<ParameterDescriptor>();

            var functionName = NormalizeMethodName(manifest.Entrypoint);

            var parameterList = string.Join(", ",
                parameters.Select((p, i) => NormalizeIdentifier(p.Name, $"arg{i}")));

            var sb = new StringBuilder();

            sb.AppendLine($"function {functionName}({parameterList}) {{");
            sb.AppendLine("    // TODO: implement");
            sb.AppendLine("}");

            return sb.ToString();
        }

        private static string NormalizeMethodName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
                return "solution";

            var trimmed = name.Trim();

            if (trimmed.Length == 1)
                return trimmed.ToLowerInvariant();

            return char.ToLowerInvariant(trimmed[0]) + trimmed[1..];
        }

        private static string NormalizeIdentifier(string? name, string fallback)
        {
            if (string.IsNullOrWhiteSpace(name))
                return fallback;

            var sb = new StringBuilder();
            foreach (var c in name.Trim())
            {
                if (char.IsLetterOrDigit(c) || c == '_')
                    sb.Append(c);
                else
                    sb.Append('_');
            }

            var result = sb.Length == 0 ? fallback : sb.ToString();

            if (char.IsDigit(result[0]))
                result = "_" + result;

            return result;
        }
    }
}