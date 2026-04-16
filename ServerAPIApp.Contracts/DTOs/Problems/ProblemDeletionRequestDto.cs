namespace ServerAPIApp.Contracts.DTOs.Problems
{
    public record ProblemDeletionRequestDto(Guid InitiatorId, string Reason);
}
