namespace ServerAPIApp.Contracts.DTOs
{
    public record AdminUserDto(string Name, string Email, DateTimeOffset CreatedAt, Guid CreatedBy, DateTimeOffset ModifiedAt,
        Guid ModifiedBy, bool IsDeleted, DateTimeOffset? DeletionScheduledAt, DateTimeOffset? DeletionDeadline, Guid? DeletionInitiatorId,
        List<string> Roles);
}
