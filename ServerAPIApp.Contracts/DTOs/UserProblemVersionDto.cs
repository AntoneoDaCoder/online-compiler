using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Contracts.DTOs
{
    public record UserProblemVersionDto(Guid ProblemId, string Statement, int TotalTests)
    {
        public static UserProblemVersionDto From(ProblemVersionEntity entity)
        {
            var dto = new UserProblemVersionDto(entity.ProblemId, entity.Statement, entity.TotalTests);

            return dto;
        }
    }
}
