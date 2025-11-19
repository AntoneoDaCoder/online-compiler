using ServerAPIApp.Core.UseCases.Languages;
using ServerAPIApp.Core.UseCases.Problems;
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
    }
}
