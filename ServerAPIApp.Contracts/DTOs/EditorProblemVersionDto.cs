using ServerAPIApp.Domain.Entities;
using Shared.DTOs;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace ServerAPIApp.Contracts.DTOs
{
    public class EditorProblemVersionDto
    {
        public Guid ProblemId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public Guid CreatedBy { get; set; }
        public string Statement { get; set; }
        public int TotalTests { get; set; }
        public IEnumerable<string> SupportedLanguages { get; set; } = Enumerable.Empty<string>();
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
                SupportedLanguages = entity.SupportedLanguages.Select(x => x.Language.Code).ToList(),
                TestManifest = manifest
            };

            return dto;
        }
    }
}
