using Shared.DTOs;

namespace ServerAPIApp.Contracts.DTOs
{
    public record ProblemVersionDto(string Statement, int TotalTests, ManifestDto? TestManifest);
}
