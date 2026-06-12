using System.ComponentModel;
using System.Text.Json.Serialization;

namespace Shared.DTOs.ManifestHelpers
{
    public class ParameterDescriptor
    {
        public string Name { get; set; } = "";
        public TypeDescriptor Type { get; set; } = new();
    }

}
