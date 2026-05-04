using ServerAPIApp.Core.Abstractions;
using Shared.DTOs;
using Shared.DTOs.ManifestHelpers;
using System.Text;

namespace ServerAPIApp.Core.Helpers.TemplateGenerators
{
    public class KotlinTemplateGenerator : ITemplateGenerator
    {
        public string LanguageCode => "kotlin";

        public string BuildTemplate(ManifestDto manifest)
        {
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));
            if (manifest.Signature == null) throw new ArgumentNullException(nameof(manifest.Signature));

            var signature = manifest.Signature;
            var functionName = NormalizeMethodName(manifest.Entrypoint);

            var parameters = signature.Parameters ?? new List<ParameterDescriptor>();
            var parameterList = string.Join(", ",
                parameters.Select((p, i) =>
                    $"{NormalizeIdentifier(p.Name, $"arg{i}")}: {RenderTypeName(p.Type)}"));

            var returnType = RenderTypeName(signature.ReturnType);

            var sb = new StringBuilder();
            sb.Append($"fun {functionName}({parameterList})");

            if (!string.Equals(returnType, "Unit", StringComparison.OrdinalIgnoreCase))
                sb.Append($": {returnType}");

            sb.AppendLine(" {");
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

        private static string RenderTypeName(TypeDescriptor? t)
        {
            if (t == null) return "Any";

            if (string.Equals(t.Kind, "primitive", StringComparison.OrdinalIgnoreCase))
            {
                return t.Name?.ToLowerInvariant() switch
                {
                    "int" => "Int",
                    "long" => "Long",
                    "double" => "Double",
                    "string" => "String",
                    "bool" => "Boolean",
                    "boolean" => "Boolean",
                    "void" => "Unit",
                    "float" => "Float",
                    "decimal" => "Double",
                    "char" => "Char",
                    _ => t.Name ?? "Any"
                };
            }

            if (string.Equals(t.Kind, "array", StringComparison.OrdinalIgnoreCase))
            {
                var items = t.Items;
                if (items == null)
                    return "Array<Any>";

                if (string.Equals(items.Kind, "primitive", StringComparison.OrdinalIgnoreCase))
                {
                    return items.Name?.ToLowerInvariant() switch
                    {
                        "int" => "IntArray",
                        "long" => "LongArray",
                        "double" => "DoubleArray",
                        "bool" => "BooleanArray",
                        "boolean" => "BooleanArray",
                        "string" => "Array<String>",
                        "float" => "FloatArray",
                        "char" => "CharArray",
                        _ => $"Array<{RenderTypeName(items)}>"
                    };
                }

                return $"Array<{RenderTypeName(items)}>";
            }

            if (string.Equals(t.Kind, "nullable", StringComparison.OrdinalIgnoreCase))
            {
                return $"{RenderTypeName(t.Of)}?";
            }

            if (string.Equals(t.Kind, "class", StringComparison.OrdinalIgnoreCase))
            {
                return string.IsNullOrWhiteSpace(t.Name) ? "Any" : t.Name!;
            }

            return t.Name ?? "Any";
        }
    }
}