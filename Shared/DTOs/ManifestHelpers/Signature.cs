using System.Reflection.Metadata;
using System.Text.Json.Serialization;

namespace Shared.DTOs.ManifestHelpers
{
    public class Signature
    {
        public string ReturnType { get; set; } = "void";
        public List<ParameterDescriptor> Parameters { get; set; } = new();
    }
}
