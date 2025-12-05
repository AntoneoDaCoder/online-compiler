using Newtonsoft.Json.Linq;
using Shared.DTOs.ManifestHelpers;

namespace Runners.Shared.CodeWrappers.CSharp
{
    public static class CSharpTokenParser
    {
        public static string Render(JToken token, TypeDescriptor? type = null)
        {
            if (token == null) return "null";
            if (token.Type == JTokenType.Null) return "null";

            switch (token.Type)
            {
                case JTokenType.Integer:
                    return token.Value<long>().ToString();
                case JTokenType.Float:
                    return token.Value<double>().ToString(System.Globalization.CultureInfo.InvariantCulture);
                case JTokenType.Boolean:
                    return token.Value<bool>() ? "true" : "false";
                case JTokenType.String:
                    var s = token.Value<string>() ?? "";
                    s = s.Replace("\"", "\"\""); // escape double quotes for verbatim string
                    return $"@\"{s}\"";
                case JTokenType.Array:
                    var arr = token.Children().ToArray();
                    var items = arr.Select(t => Render(t, type?.Items)).ToArray();
                    var itemType = type != null && type.Kind == "array" && type.Items != null ? RenderTypeName(type.Items) : "object";
                    return $"new {itemType}[] {{ {string.Join(", ", items)} }}";
                case JTokenType.Object:
                    var obj = (JObject)token;
                    if (type != null && type.Kind == "class" && !string.IsNullOrWhiteSpace(type.Name))
                    {
                        var props = obj.Properties().Select(p => $"{p.Name} = {Render(p.Value, null)}");
                        return $"new {type.Name} {{ {string.Join(", ", props)} }}";
                    }
                    else
                    {
                        // fallback: create anonymous dictionary
                        var entries = obj.Properties().Select(p => $"{{\"{p.Name}\", {Render(p.Value, null)}}}");
                        return $"new System.Collections.Generic.Dictionary<string, object> {{ {string.Join(", ", entries)} }}";
                    }
                default:
                    return $"@\"{token.ToString().Replace("\"", "\"\"")}\"";
            }
        }

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
