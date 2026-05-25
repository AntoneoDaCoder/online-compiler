using ServerAPIApp.Core.Abstractions;
using Shared.DTOs;
using Shared.DTOs.ManifestHelpers;
using System.Text;

namespace ServerAPIApp.Core.Helpers.TemplateGenerators
{
    public class JavaTemplateGenerator : ITemplateGenerator
    {
        public string LanguageCode => "java";

        public string BuildTemplate(ManifestDto manifest)
        {
            if (manifest == null) throw new ArgumentNullException(nameof(manifest));
            if (manifest.Signature == null) throw new ArgumentNullException(nameof(manifest.Signature));

            var signature = manifest.Signature;
            var methodName = NormalizeMethodName(manifest.Entrypoint); // получение нормализованного имени метода

            var parameters = signature.Parameters ?? new List<ParameterDescriptor>();
            var parameterList = string.Join(", ",
                parameters.Select((p, i) =>
                    $"{RenderTypeName(p.Type)} {NormalizeIdentifier(p.Name, $"arg{i}")}")); // получение списка нормализованных параметров метода с их типами

            var returnType = RenderTypeName(signature.ReturnType); // получение типа возвращаемого значения ожидаемой подпрограммы решения

            var sb = new StringBuilder();

            sb.Append("public static ");

            sb.Append($"{returnType} {methodName}({parameterList})");

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
            if (t == null) return "Object";

            if (string.Equals(t.Kind, "primitive", StringComparison.OrdinalIgnoreCase))
            {
                return t.Name?.ToLowerInvariant() switch
                {
                    "int" => "int",
                    "long" => "long",
                    "double" => "double",
                    "float" => "float",
                    "decimal" => "double",
                    "string" => "String",
                    "bool" => "boolean",
                    "boolean" => "boolean",
                    "char" => "char",
                    "void" => "void",
                    _ => t.Name ?? "Object"
                };
            }

            if (string.Equals(t.Kind, "array", StringComparison.OrdinalIgnoreCase))
                return $"{RenderTypeName(t.Items)}[]";

            if (string.Equals(t.Kind, "nullable", StringComparison.OrdinalIgnoreCase))
                return RenderTypeName(t.Of);

            if (string.Equals(t.Kind, "class", StringComparison.OrdinalIgnoreCase))
                return string.IsNullOrWhiteSpace(t.Name) ? "Object" : t.Name!;

            return "Object";
        }
    }
}