using Microsoft.AspNetCore.Authorization;
using ServerAPIApp.Core.AuthorizationRequirements;

namespace ServerAPIApp.Middlewares
{
    public class RoleHandler : AuthorizationHandler<RoleRequirement>
    {
        protected override Task HandleRequirementAsync(AuthorizationHandlerContext context, RoleRequirement requirement)
        {
            foreach (var role in requirement.Roles)
            {
                if (context.User.IsInRole(role))
                {
                    context.Succeed(requirement);

                    return Task.CompletedTask;
                }
            }

            context.Fail();

            return Task.CompletedTask;
        }
    }
}
