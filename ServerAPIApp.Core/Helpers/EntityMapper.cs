using ServerAPIApp.Contracts.DTOs;
using ServerAPIApp.Core.UseCases.Languages;
using ServerAPIApp.Core.UseCases.Problems;
using ServerAPIApp.Core.UseCases.ProblemVersions;
using ServerAPIApp.Core.UseCases.Submissions;
using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Core.Helpers
{
    public static class EntityMapper
    {
        #region LanguageEntity related mapping
        public static LanguageEntity ToEntity(this CreateLanguageCase command)
        {
            var entity = new LanguageEntity()
            {
                Id = Guid.NewGuid(),
                DisplayName = command.DisplayName,
                Code = command.Code,
            };

            return entity;
        }

        public static LanguageEntity ToEntity(this UpdateLanguageCase command)
        {
            var entity = new LanguageEntity()
            {
                Id = command.Id,
                DisplayName = command.DisplayName,
                Code = command.Code,
            };

            return entity;
        }

        public static IEnumerable<LanguageDto>? ToDto(this IEnumerable<LanguageEntity>? entities)
        {
            if (entities is null) return null;

            return entities.Select(e => LanguageDto.From(e));
        }
        #endregion

        #region ProblemEntity related mapping
        public static ProblemEntity ToEntity(this CreateProblemCase command)
        {
            var entity = new ProblemEntity()
            {
                Id = Guid.NewGuid(),
                Slug = command.Slug,
                Title = command.Title,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = command.CreatorId
            };

            return entity;
        }

        public static ProblemEntity ToEntity(this UpdateProblemCase command)
        {
            var entity = new ProblemEntity()
            {
                Id = command.ProblemId,
                Slug = command.Slug,
                Title = command.Title,
                ModifiedAt = DateTimeOffset.UtcNow,
                ModifiedBy = command.EditorId
            };

            return entity;
        }
        #endregion

        #region SubmissionEntity related mapping
        public static SubmissionEntity ToEntity(this CreateSubmissionCase command)
        {
            var entity = new SubmissionEntity()
            {
                Id = Guid.NewGuid(),
                ProblemVersionId = command.Response.VersionId,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = command.Response.UserId,
                Solution = command.Response.UserSolution,
                SolutionLanguage = command.Response.Language,
                PassedTests = command.Response.Result.PassedTests,
                TotalTests = command.Response.Result.TotalTests
            };

            return entity;
        }

        public static IEnumerable<ShortSubmissionDto>? ToDto(this IEnumerable<SubmissionEntity>? entities)
        {
            if (entities is null) return null;

            return entities.Select(e => ShortSubmissionDto.From(e));
        }
        #endregion

        #region ProblemVersionEntity related mapping
        public static (ProblemVersionEntity Entity, string? TestManifestJson) ToEntity(this CreateVersionDraftCase command)
        {
            var manifest = command.TestManifestJson;

            var draft = new ProblemVersionEntity()
            {
                Id = Guid.NewGuid(),
                ProblemId = command.ProblemId,
                CreatedAt = DateTimeOffset.UtcNow,
                CreatedBy = command.CreatedBy,
                Version = 0,
                IsDraft = true,
                IsPublished = false,
                Statement = command.Statement,
                TotalTests = command.TotalTests,
                NumSubmissions = 0
            };

            return (draft, manifest);
        }

        public static (ProblemVersionEntity Entity, string? TestManifestJson) ToEntity(this UpdateVersionDraftCase command)
        {
            var manifest = command.TestManifestJson;

            var draft = new ProblemVersionEntity()
            {
                Id = command.VersionId,
                ProblemId = command.ProblemId,
                Statement = command.Statement,
                TotalTests = command.TotalTests,
            };

            return (draft, manifest);
        }
        #endregion
    }
}
