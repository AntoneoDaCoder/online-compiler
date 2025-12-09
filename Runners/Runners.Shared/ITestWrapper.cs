using Shared.DTOs;

namespace Runners.Shared
{
    public interface ITestWrapper
    {
        string GenerateSource(ManifestDto manifest, string userCode, string entrypointContainerClass = "SolutionContainer", int defaultTimeoutMs = 2000);
    }
}

