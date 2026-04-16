using Hangfire;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.ValueGeneration.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.DAL.Contexts;
using ServerAPIApp.DAL.Repositories;
using Hangfire.PostgreSql;

namespace ServerAPIApp.DAL.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static void ConfigureDbContext(this IServiceCollection services, IConfiguration conf)
        {
            services.AddDbContext<BaseDbContext>
                (
                    options => options.UseNpgsql(conf.GetConnectionString("DbConnectionString"))
                );

            services.AddHangfire(opt =>
               opt.UsePostgreSqlStorage(
                   conn => conn.UseNpgsqlConnection(conf.GetConnectionString("DbConnectionString")
                   ),
                   new PostgreSqlStorageOptions()
                   {
                       SchemaName = "hangfire"
                   }
                   )
               );
        }

        public static void ConfigureRepositories(this IServiceCollection services)
        {
            services.AddScoped<ILanguageRepository, LanguageRepository>();

            services.AddScoped<IProblemRepository, ProblemEntityRepository>();

            services.AddScoped<IProblemVersionRepository, ProblemVersionEntityRepository>();

            services.AddScoped<IProblemVersionLanguageRepository, ProblemVersionLanguageRepository>();

            services.AddScoped<ISubmissionRepository, SubmissionRepository>();

            services.AddScoped<IUserRepository, UserRepository>();

            services.AddScoped<IProblemDeletionRequestRepository, ProblemDeletionRequestRepository>();
        }

        public static void ConfigureObjectStorage(this IServiceCollection services, IConfiguration cfg)
        {
            services.AddSingleton<IObjectStorage>
                (
                sp =>
                {
                    var endpoint = cfg["Minio:Endpoint"];
                    var access = cfg["Minio:AccessKey"];
                    var secret = cfg["Minio:SecretKey"];
                    bool useSsl = bool.Parse(cfg["Minio:UseSsl"] ?? "true");
                    return new ObjectStorage(endpoint, access, secret, useSsl);
                });
        }
    }
}
