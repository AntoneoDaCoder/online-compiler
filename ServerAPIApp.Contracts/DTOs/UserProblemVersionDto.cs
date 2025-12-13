using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Contracts.DTOs
{

    //null checks are not necessary here because this dto appears only when problem has been published (so it has an active version)
    public record UserProblemVersionDto(Guid VersionId, Guid ProblemId, string Statement, int TotalTests, IEnumerable<Guid> SupportedLanguages)
    {
        public static UserProblemVersionDto? From(ProblemVersionEntity? entity)
        {
            if (entity == null) return null;

            return new UserProblemVersionDto
                (
                entity.Id,
                entity.ProblemId,
                entity.Statement,
                entity.TotalTests,
                entity.SupportedLanguages.Select(x => x.LanguageId).ToList()
                );
        }
    }
}
