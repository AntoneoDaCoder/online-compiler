using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Contracts.DTOs.Problems
{
    public record ProblemDeletionRequestDto(Guid Id, Guid InitiatorId, Guid ProblemId, string Reason, string ProblemSlug, string ProblemTitle)
    {
        public static ProblemDeletionRequestDto? From(ProblemDeletionRequestEntity? entity)
        {
            if (entity is null || entity.Problem is null)
                return null;

            return new ProblemDeletionRequestDto(entity.Id, entity.InitiatorId, entity.ProblemId, entity.Reason, entity.Problem.Slug, entity.Problem.Title);
        }
    }
}
