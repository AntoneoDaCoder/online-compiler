using k8s;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ServerAPIApp.Core.Abstractions;
using ServerAPIApp.Core.Configs;
using ServerAPIApp.Core.Services;
using ServerAPIApp.DAL.Extensions;
using System.Reflection;

namespace ServerAPIApp.Core.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection RegisterServices(this IServiceCollection services, IConfiguration config)
        {
            services.ConfigureDbContext();
            services.ConfigureObjectStorage();
            services.ConfigureRepositories();

            //TODO:
            // add google auth service registration here
            //

            services.AddDataProtection().SetApplicationName("ServerAPIApp");
            services.AddSingleton<ISecretProtector, DataProtectionSecretProtector>();
            services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
            services.AddScoped<IJwtTokenService, JwtTokenService>();

            services.AddMediatR
                (
                cfg => cfg.RegisterServicesFromAssembly(Assembly.GetExecutingAssembly())
                );

            return services;
        }
    }
}
