using ServerAPIApp.Core.Abstractions;
using Shared.DTOs;
using Shared.DTOs.ManifestHelpers;
using System.Text;
using Shared.Helpers.TypeNameRenderers;

namespace ServerAPIApp.Core.Helpers.TemplateGenerators
{
    public class CSharpTemplateGenerator : ITemplateGenerator
    {
        public string LanguageCode => "csharp";

        public string BuildTemplate(ManifestDto manifest)
        {
            var signature = manifest.Signature;
            var methodName = ToPascalCase(manifest.Entrypoint);

            if (signature == null) throw new ArgumentNullException(nameof(signature));

            var returnType = CSharpTypeNameRenderer.RenderTypeName(signature.ReturnType);
            var parameters = signature.Parameters ?? new List<ParameterDescriptor>();

            var parameterList = string.Join(", ",
                parameters.Select((p, i) => $"{CSharpTypeNameRenderer.RenderTypeName(p.Type)} {NormalizeIdentifier(p.Name, $"arg{i}")}"));

            var sb = new StringBuilder();
            sb.AppendLine($"public static {returnType} {methodName}({parameterList})");
            sb.AppendLine("{");

            sb.AppendLine("    // TODO: implement");

            sb.AppendLine("}");

            return sb.ToString();
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

        private static string ToPascalCase(string word)
        {
            if (string.IsNullOrEmpty(word))
                return word;

            if (word.Length == 1)
                return word.ToUpperInvariant();

            return char.ToUpperInvariant(word[0]) + word[1..];
        }
    }
}