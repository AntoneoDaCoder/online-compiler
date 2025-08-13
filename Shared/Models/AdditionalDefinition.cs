namespace Shared.Models
{
    public class AdditionalDefinition
    {
        public string Language { get; set; } = string.Empty;
        public string Value { get; set; } = string.Empty;

        public AdditionalDefinition() { }

        public AdditionalDefinition(AdditionalDefinition source)
        {
            Language = source.Language;
            Value = source.Value;
        }
    }
}
