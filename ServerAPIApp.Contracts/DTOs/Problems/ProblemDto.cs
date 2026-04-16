using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Contracts.DTOs.Problems
{
    public record ProblemDto(Guid Id, Guid? VersionLink, string Slug, string Title, string Status, string? Reason, UserProblemVersionDto? LatestVersion)
    {
        public static ProblemDto From(Guid id, Guid? versionLink, string slug, string title, string status, string? reason, UserProblemVersionDto? latestVersion)
        {
            return new ProblemDto(id, versionLink, slug, title, status, reason, latestVersion);
        }

        public static ProblemDto From(ProblemEntity entity)
        {
            var problemStatus = (entity.IsDeleted || entity.LastPublishedVersionId == null) ? "Unlisted" : "Listed";

            string? reason = null;

            IEnumerable<Guid> supportedLanguages = [];

            if (entity.LastPublishedVersionId == null)
                reason = "No version";
            else
            {
                //EF Core sets this property to null if lastpublishedid is null, so if its not null there is actually such a version with languages in the db
                supportedLanguages = entity.LastPublishedVersion!.SupportedLanguages.Select(x => x.LanguageId).ToList();
            }

            //lastpublishedid can be null if there is no version for this problem or it has been deleted, so to mitigate a shit ton of if-else, we check if
            //its null and then check if it has been actually deleted
            if (entity.IsDeleted)
                reason = "Deleted";

            return new ProblemDto
                (
                entity.Id,
                entity.LastPublishedVersionId,
                entity.Slug,
                entity.Title,
                problemStatus,
                reason,
                UserProblemVersionDto.From(entity.LastPublishedVersion)
                );
        }
    }
}
