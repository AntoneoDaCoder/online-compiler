using ServerAPIApp.Middlewares;

namespace ServerAPIApp.Extensions
{
    public static class ApplicationBuilderExtensions
    {
        public static void ConfigureMiddleware(this IApplicationBuilder app)
        {
            app.UseMiddleware<ExceptionMiddleware>();
        }
    }
}
