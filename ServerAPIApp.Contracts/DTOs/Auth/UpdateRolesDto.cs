namespace ServerAPIApp.Contracts.DTOs.Auth
{
    public record UpdateRolesDto(IEnumerable<string>? RolesToRemove = null, IEnumerable<string>? RolesToAdd = null);
}
