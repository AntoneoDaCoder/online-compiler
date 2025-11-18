using System.Reflection.Metadata;
using System.Text.Json.Serialization;

namespace ServerAPIApp.Contracts.DTOs.ManifestHelpers
{
    public class Signature
    {
        public string ReturnType { get; set; } = "void";
        public List<ParameterDescriptor> Parameters { get; set; } = new();
    }
}
