using Shared.DTOs;

namespace ServerAPIApp.Contracts.DTOs.Problems
{
    public record ProblemVersionDto(string Statement, int TotalTests, ManifestDto? TestManifest);
}
