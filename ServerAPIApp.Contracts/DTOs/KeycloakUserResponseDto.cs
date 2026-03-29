namespace ServerAPIApp.Contracts.DTOs
{
    public record KeycloakUserResponseDto(string Id, string Email, bool EmailVerified, long CreatedTimestamp);
}
