using k8s.KubeConfigModels;
using Microsoft.IdentityModel.JsonWebTokens;
using System.Security.Claims;

namespace ServerAPIApp.Extensions
{
    public static class JsonWebTokenHandlerExtensions
    {
        public static Guid ExtractUserId(this JsonWebTokenHandler handler, string token)
        {
            var jwtToken = handler.ReadJsonWebToken(token);

            return Guid.Parse(jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
                ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value);
        }
    }
}
