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
                //needed because on one tab on the UI we don't need the languages and the latest version, so its not included
                if (entity.LastPublishedVersion is not null)
                    supportedLanguages = entity.LastPublishedVersion.SupportedLanguages.Select(x => x.LanguageId).ToList();
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
