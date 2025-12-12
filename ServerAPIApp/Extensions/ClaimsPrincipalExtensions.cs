using System.Security.Claims;

namespace ServerAPIApp.Extensions
{
    public static class ClaimsPrincipalExtensions
    {
        public static IEnumerable<string> GetRoles(this ClaimsPrincipal user)
        {
            return user.Claims.Where(claim => claim.Type == ClaimTypes.Role).Select(c => c.Value).ToList();
        }
    }
}
