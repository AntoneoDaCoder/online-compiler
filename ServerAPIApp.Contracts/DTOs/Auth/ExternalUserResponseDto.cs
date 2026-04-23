namespace ServerAPIApp.Contracts.DTOs.Auth
{
    public record ExternalUserResponseDto(string Id, string Email, string Username, bool EmailVerified, long CreatedTimestamp);
}
