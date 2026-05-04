using Shared.DTOs.ManifestHelpers;

namespace Shared.Helpers.TypeNameRenderers
{
    public static class CSharpTypeNameRenderer
    {
        public static string RenderTypeName(TypeDescriptor? t)
        {
            if (t == null) return "object";
            if (t.Kind == "primitive")
            {
                return t.Name switch
                {
                    "int" => "int",
                    "long" => "long",
                    "double" => "double",
                    "string" => "string",
                    "bool" => "bool",
                    _ => t.Name ?? "object"
                };
            }
            if (t.Kind == "array") return $"{RenderTypeName(t.Items)}[]";
            if (t.Kind == "class") return t.Name ?? "object";
            if (t.Kind == "nullable") return $"{RenderTypeName(t.Of)}?";
            return "object";
        }
    }
}
