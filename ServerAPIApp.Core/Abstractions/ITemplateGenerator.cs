using Shared.DTOs;
using Shared.DTOs.ManifestHelpers;

namespace ServerAPIApp.Core.Abstractions
{
    public interface ITemplateGenerator
    {
        string LanguageCode { get; }
        string BuildTemplate(ManifestDto manifest);
    }
}
