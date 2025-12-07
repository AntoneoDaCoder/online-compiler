using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using ServerAPIApp.Core.Abstractions;
using ServerAPIApp.Core.Configs;
using ServerAPIApp.Core.Services;
using ServerAPIApp.DAL.Extensions;
using ServerAPIApp.Core.AuthorizationRequirements;
using System.Reflection;
using System.Security.Claims;
using System.Text;

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

            var jwtSection = config.GetSection("JwtSettings");
            var jwtSettings = jwtSection.Get<JwtSettings>();

            services.Configure<JwtSettings>(jwtSection);

            services.AddAuthorizationBuilder()
            .AddPolicy("AdminAccess", policy => policy
                   .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                   .AddRequirements(new RoleRequirement(["Admin"])))
            .AddPolicy("DefaultAccess", policy => policy
                   .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                   .AddRequirements(new RoleRequirement(["Admin", "User"])));


            services.AddAuthentication(opt =>
            {
                opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateAudience = true,
                    ValidateIssuer = true,
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(
                                                Encoding.UTF8.GetBytes(Environment.GetEnvironmentVariable("JWT_SECRET")!)
                                                ),
                    ValidateLifetime = true,
                    ValidIssuer = jwtSettings.ValidIssuer,
                    ValidAudience = jwtSettings.ValidAudience,
                    RoleClaimType = ClaimTypes.Role,
                };
            });

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
