using Shared.DTOs.ManifestHelpers;

namespace Shared.Helpers.TypeNameRenderers
{
    public static class TypeScriptTypeNameRenderer
    {
        public static string RenderTypeName(TypeDescriptor? t)
        {
            if (t == null) return "any";

            var kind = t.Kind?.ToLowerInvariant();

            if (kind == "primitive")
                return MapPrimitive(t.Name);

            if (kind == "array")
                return $"{RenderTypeName(t.Items)}[]";

            if (kind == "class")
                return string.IsNullOrWhiteSpace(t.Name) ? "any" : t.Name;

            if (kind == "nullable")
            {
                var inner = RenderTypeName(t.Of);
                return inner == "any" ? "any" : $"{inner} | null";
            }

            return "any";
        }

        private static string MapPrimitive(string? name) => name?.ToLowerInvariant() switch
        {
            "void" => "void",
            "string" => "string",
            "int" => "number",
            "long" => "number",
            "double" => "number",
            "float" => "number",
            "decimal" => "number",
            "bool" => "boolean",
            "char" => "string",
            _ => "any"
        };
    }
}
