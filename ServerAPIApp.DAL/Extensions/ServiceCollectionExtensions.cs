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

            using (var conn = new NpgsqlConnection(conf.GetConnectionString("DbConnectionString")))
            {
                conn.Open();

                using (var cmd = new NpgsqlCommand("CREATE SCHEMA IF NOT EXISTS hangfire", conn))
                {
                    cmd.ExecuteNonQuery();
                }
            }

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
            var minioConfSection = cfg.GetSection("Minio");

            services.Configure<MinioConfiguration>(minioConfSection);

            services.AddSingleton<IObjectStorage, ObjectStorage>();
        }
    }
}
