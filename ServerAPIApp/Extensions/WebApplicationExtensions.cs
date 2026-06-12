using Microsoft.EntityFrameworkCore;
using ServerAPIApp.DAL.Contexts;

namespace ServerAPIApp.Extensions
{
    public static class WebApplicationExtensions
    {
        public static WebApplication MigrateDatabase(this WebApplication app)
        {
            using var scope = app.Services.CreateScope();

            var sp = scope.ServiceProvider;

            try
            {
                var context = sp.GetRequiredService<BaseDbContext>();

                context.Database.Migrate();
            }
            catch (Exception) { }

            return app;
        }
    }
}
