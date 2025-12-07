using Microsoft.IdentityModel.JsonWebTokens;
using ServerAPIApp.Extensions;

namespace ServerAPIApp.Helpers
{
    public static class IdExtractionHelper
    {
        public static Guid GetIdFromJwtToken(HttpContext httpContext)
        {
            var token = httpContext.GetBearerToken();

            var handler = new JsonWebTokenHandler();

            return handler.ExtractUserId(token);
        }
    }
}
