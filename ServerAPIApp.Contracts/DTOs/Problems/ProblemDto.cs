using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Contracts.DTOs.Problems
{
    public record ProblemDto(Guid Id, Guid? VersionLink, string Slug, string Title, bool IsPublished, bool IsDeleted, UserProblemVersionDto? LatestVersion)
    {
        public static ProblemDto From(Guid id, Guid? versionLink, string slug, string title, bool isPublished, bool isDeleted, UserProblemVersionDto? latestVersion)
        {
            return new ProblemDto(id, versionLink, slug, title, isPublished, isDeleted, latestVersion);
        }

        public static ProblemDto From(ProblemEntity entity)
        {
            var isPublished = true;

            List<Guid> supportedLanguages = [];

            if (entity.LastPublishedVersionId == null)
                isPublished = false;
            else
            {
                //EF Core sets this property to null if lastpublishedid is null, so if its not null there is actually such a version with languages in the db
                supportedLanguages = entity.LastPublishedVersion!.SupportedLanguages.Select(x => x.LanguageId).ToList();
            }

            return new ProblemDto
                (
                entity.Id,
                entity.LastPublishedVersionId,
                entity.Slug,
                entity.Title,
                isPublished,
                entity.IsDeleted,
                UserProblemVersionDto.From(entity.LastPublishedVersion)
                );
        }
    }
}
