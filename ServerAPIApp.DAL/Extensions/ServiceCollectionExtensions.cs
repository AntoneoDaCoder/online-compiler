using Hangfire;
using Hangfire.PostgreSql;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Npgsql;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.DAL.Confs;
using ServerAPIApp.DAL.Contexts;
using ServerAPIApp.DAL.Repositories;

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

            services.AddHangfire((sp, cfg) =>
            {
                cfg.UsePostgreSqlStorage(opts =>
                    opts.UseNpgsqlConnection(conf.GetConnectionString("DbConnectionString")),
                    new PostgreSqlStorageOptions
                    {
                        SchemaName = "hangfire",
                        PrepareSchemaIfNecessary = true,
                        StartupConnectionMaxRetries = 0,
                        AllowDegradedModeWithoutStorage = false
                    });
            });

            services.AddHangfireServer();
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
            var minioConfSection = cfg.GetSection("Minio");

            services.Configure<MinioConfiguration>(minioConfSection);

            services.AddSingleton<IObjectStorage, ObjectStorage>();
        }
    }
}
