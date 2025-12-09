using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Shared.DTOs.ManifestHelpers;

namespace Runners.Shared.CodeWrappers.Typescript
{
    public static class TypeScriptTokenParser
    {
        public static string Render(JToken token, TypeDescriptor? type = null)
        {
            if (token == null) return "null";
            if (token.Type == JTokenType.Null) return "null";

            var kind = type?.Kind?.ToLowerInvariant();
            switch (token.Type)
            {
                case JTokenType.Integer:
                    return token.Value<long>().ToString(System.Globalization.CultureInfo.InvariantCulture);
                case JTokenType.Float:
                    return token.Value<double>().ToString("G17", System.Globalization.CultureInfo.InvariantCulture);
                case JTokenType.Boolean:
                    return token.Value<bool>() ? "true" : "false";
                case JTokenType.String:
                    return JsonConvert.SerializeObject(token.Value<string>() ?? "");
                case JTokenType.Array:
                    {
                        var arr = token.Children().ToArray();
                        if (kind == "class" && string.Equals(type?.Name, "Set", StringComparison.OrdinalIgnoreCase))
                        {
                            var items = arr.Select(t => Render(t, type?.Items)).ToArray();
                            return $"new Set([{string.Join(", ", items)}])";
                        }
                        var itemsRendered = arr.Select(t => Render(t, type?.Items)).ToArray();
                        return $"[{string.Join(", ", itemsRendered)}]";
                    }
                case JTokenType.Object:
                    {
                        if (kind == "class" && string.Equals(type?.Name, "Map", StringComparison.OrdinalIgnoreCase))
                        {
                            var obj = (JObject)token;
                            var pairs = obj.Properties().Select(p => $"[{JsonConvert.SerializeObject(p.Name)}, {Render(p.Value, null)}]");
                            return $"new Map([{string.Join(", ", pairs)}])";
                        }
                        var obj2 = (JObject)token;
                        var props = obj2.Properties().Select(p => $"{JsonConvert.SerializeObject(p.Name)}: {Render(p.Value, null)}");
                        return $"{{ {string.Join(", ", props)} }}";
                    }
                default:
                    return JsonConvert.SerializeObject(token.ToString());
            }
        }
    }
}
