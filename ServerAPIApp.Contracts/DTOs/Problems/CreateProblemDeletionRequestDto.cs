namespace ServerAPIApp.Contracts.DTOs.Problems
{
    public record CreateProblemDeletionRequestDto(Guid InitiatorId, string Reason);
}
