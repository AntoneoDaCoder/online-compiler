namespace ServerAPIApp.Extensions
{
    public static class HttpContextExtensions
    {
        public static string GetBearerToken(this HttpContext httpContext)
        {
            var authorizationHeader = httpContext.Request.Headers["Authorization"].ToString();

            return authorizationHeader.Substring("Bearer ".Length).Trim();
        }
    }
}
