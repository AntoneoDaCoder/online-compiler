using ServerAPIApp.Domain.Entities;
using Shared.DTOs;

namespace ServerAPIApp.Contracts.DTOs
{
    public class EditorProblemVersionDto
    {
        public Guid ProblemId { get; set; }
        public DateTimeOffset? CreatedAt { get; set; }
        public Guid? CreatedBy { get; set; }
        public string Statement { get; set; }
        public int TotalTests { get; set; }
        public int Version { get; set; }
        public IEnumerable<Guid> SupportedLanguages { get; set; } = [];
        public ManifestDto? TestManifest { get; set; }

        public static EditorProblemVersionDto From(ProblemVersionEntity entity, ManifestDto? manifest)
        {
            var dto = new EditorProblemVersionDto()
            {
                ProblemId = entity.ProblemId,
                CreatedAt = entity.CreatedAt,
                CreatedBy = entity.CreatedBy,
                Statement = entity.Statement,
                TotalTests = entity.TotalTests,
                Version = entity.Version,
                SupportedLanguages = entity.SupportedLanguages.Select(x => x.LanguageId).ToList(),
                TestManifest = manifest
            };

            return dto;
        }
    }
}
