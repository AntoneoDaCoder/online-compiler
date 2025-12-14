using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using ServerAPIApp.Core.Abstractions;
using ServerAPIApp.Core.AuthorizationRequirements;
using ServerAPIApp.Core.Configs;
using ServerAPIApp.Core.Services;
using ServerAPIApp.DAL.Extensions;
using System.Reflection;
using System.Security.Claims;
using System.Text;

namespace ServerAPIApp.Core.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection RegisterServices(this IServiceCollection services, IConfiguration config)
        {
            services.ConfigureDbContext(config);
            services.ConfigureObjectStorage(config);
            services.ConfigureRepositories();

            var googleSection = config.GetSection("GoogleAuth");
            var googleSettings = googleSection.Get<GoogleAllowedAudiences>();
            services.Configure<GoogleAllowedAudiences>(googleSection);

            services.AddScoped<IGoogleAuthTokenValidator, GoogleAuthTokenValidator>();

            var jwtSection = config.GetSection("JwtSettings");
            var jwtSettings = jwtSection.Get<JwtSettings>();

            services.Configure<JwtSettings>(jwtSection);

            services.AddAuthorizationBuilder()
            .AddPolicy("AdminAccess", policy => policy
                   .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                   .AddRequirements(new RoleRequirement(["Admin"])))
            .AddPolicy("DefaultAccess", policy => policy
                   .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                   .AddRequirements(new RoleRequirement(["Admin", "User"])))
            .AddPolicy("EditorAccess", policy => policy
                   .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                   .AddRequirements(new RoleRequirement(["Admin", "Editor"])));


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

            services.Configure<SecretProtectionOptions>(config.GetSection("SecretProtector"));
            services.AddSingleton<ISecretProtector>(sp =>
            {
                var options = sp.GetRequiredService<IOptions<SecretProtectionOptions>>().Value;

                if (string.IsNullOrWhiteSpace(options.FixedKeyBase64))
                    throw new InvalidOperationException("SecretProtector:FixedKeyBase64 must be set in configuration.");

                var key = Convert.FromBase64String(options.FixedKeyBase64);

                if (!string.Equals(options.Algorithm, "AesGcm", StringComparison.OrdinalIgnoreCase))
                    throw new NotSupportedException($"Algorithm '{options.Algorithm}' is not supported.");

                return new SecretProtector(key);
            });

            services.AddSingleton<ISecretProtector, SecretProtector>();
            services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
            services.AddScoped<IJwtTokenService, JwtTokenService>();

            services.AddMediatR
                (
                cfg => cfg.RegisterServicesFromAssembly(typeof(JwtTokenService).Assembly)
                );

            return services;
        }
    }
}
