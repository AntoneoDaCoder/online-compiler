namespace ServerAPIApp.Contracts.DTOs.Auth
{
    public record ExternalUserResponseDto(string Id, string Email, bool EmailVerified, long CreatedTimestamp);
}
