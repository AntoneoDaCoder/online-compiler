using System.Reflection.Metadata;
using System.Text.Json.Serialization;

namespace Shared.DTOs.ManifestHelpers
{
    public class Signature
    {
        public TypeDescriptor ReturnType { get; set; } = new() { Kind = "primitive", Name = "void" };
        public List<ParameterDescriptor> Parameters { get; set; } = new();
    }
}
