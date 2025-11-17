using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.DAL.Contexts;
using ServerAPIApp.Domain.Entities;
using ServerAPIApp.DAL.Repositories;

namespace ServerAPIApp.DAL.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void ConfigureDbContext(this IServiceCollection services)
        {
            services.AddDbContext<BaseDbContext>
                (
                    options => options.UseNpgsql(Environment.GetEnvironmentVariable("CONNECTION_STRING"))
                );

            services.AddIdentity<UserEntity, IdentityRole<Guid>>
                (
                    options =>
                    {
                        options.Password.RequireDigit = true;
                        options.Password.RequireUppercase = true;
                        options.Password.RequiredLength = 10;
                        options.Password.RequireNonAlphanumeric = false;
                        options.User.RequireUniqueEmail = true;
                    }
                )
                .AddEntityFrameworkStores<BaseDbContext>()
                .AddDefaultTokenProviders();
        }

        public static void ConfigureRepositories(this IServiceCollection services)
        {
            services.AddScoped<ILanguageRepository, LanguageRepository>();

            services.AddScoped<IProblemRepository, ProblemEntityRepository>();

            services.AddScoped<IProblemVersionRepository, ProblemVersionEntityRepository>();

            services.AddScoped<IProblemVersionLanguageRepository, ProblemVersionLanguageRepository>();

            services.AddScoped<ISubmissionRepository, SubmissionRepository>();

            services.AddScoped<IUserRepository, UserRepository>();
        }
    }
}
