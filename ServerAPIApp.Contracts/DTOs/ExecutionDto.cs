using ServerAPIApp.Domain.Entities;
using Shared.DTOs;

namespace ServerAPIApp.Contracts.DTOs
{
    public record ExecutionDto(ProblemVersionEntity Entity, ManifestDto TestManifest)
    {
        public static ExecutionDto From(ProblemVersionEntity entity, ManifestDto manifest)
        {
            return new ExecutionDto(entity, manifest);
        }
    }
}
