using Microsoft.AspNetCore.Authorization;

namespace ServerAPIApp.Core.AuthorizationRequirements
{
    public record RoleRequirement(IEnumerable<string> Roles) : IAuthorizationRequirement;
}
