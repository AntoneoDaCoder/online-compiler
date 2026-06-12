using System.Text.Json.Serialization;

namespace Shared.DTOs.ManifestHelpers
{
    public class TypeDescriptor
    {
        public string Kind { get; set; } = "primitive"; // "primitive","array","class","nullable"
        public string? Name { get; set; } // primitive name or class FQN
        public TypeDescriptor? Items { get; set; } // for arrays
        public TypeDescriptor? Of { get; set; } // for nullable
    }
}
