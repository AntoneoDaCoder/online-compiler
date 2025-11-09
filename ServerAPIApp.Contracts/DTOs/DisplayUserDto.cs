namespace ServerAPIApp.Contracts.DTOs
{
    public record DisplayUserDto(string Name, DateTimeOffset RegisterDate, string Email);
}
