using System.ComponentModel;
using System.Text.Json.Serialization;

namespace ServerAPIApp.Contracts.DTOs.ManifestHelpers
{
    public class ParameterDescriptor
    {
        public string Name { get; set; } = "";
        public TypeDescriptor Type { get; set; } = new();
    }

}
