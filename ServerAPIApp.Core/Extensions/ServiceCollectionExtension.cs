using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;
using ServerAPIApp.Core.Abstractions;
using ServerAPIApp.Core.AuthorizationRequirements;
using ServerAPIApp.Core.Configs;
using ServerAPIApp.Core.PipelineBehaviours;
using ServerAPIApp.Core.Services;
using ServerAPIApp.Core.UseCaseHandlers.Users;
using ServerAPIApp.Core.Validators.Problems;
using ServerAPIApp.DAL.Extensions;
using ServerAPIApp.Domain.Constants;

namespace ServerAPIApp.Core.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection RegisterServices(this IServiceCollection services, IConfiguration config)
        {
            services.ConfigureDbContext(config);
            services.ConfigureObjectStorage(config);
            services.ConfigureRepositories();

            services.AddAuthorizationBuilder()
            .AddPolicy("AdminAccess", policy => policy
                   .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                   .AddRequirements(new RoleRequirement([UserRelatedConstants.AdminRoleName])))
            .AddPolicy("DefaultAccess", policy => policy
                   .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                   .AddRequirements(new RoleRequirement([UserRelatedConstants.AdminRoleName, UserRelatedConstants.DefaultUserRole])))
            .AddPolicy("EditorAccess", policy => policy
                   .AddAuthenticationSchemes(JwtBearerDefaults.AuthenticationScheme)
                   .AddRequirements(new RoleRequirement([UserRelatedConstants.AdminRoleName, UserRelatedConstants.EditorRoleName])));

            var keycloakConf = config.GetSection("KeycloakConfiguration");
            services.Configure<KeycloakConfiguration>(keycloakConf);
            var keycloakSettings = keycloakConf.Get<KeycloakConfiguration>();

            services.AddHttpClient<IExternalAuthService, KeycloakService>((sp, client) =>
            {
                client.BaseAddress = new Uri(keycloakSettings.BaseUrl);
            });

            services.AddAuthentication(opt =>
            {
                opt.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                opt.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.MetadataAddress =
                  keycloakSettings.BaseUrl + "/realms/" + keycloakSettings.Realm + "/.well-known/openid-configuration";

                options.RequireHttpsMetadata = false;

                options.Events = new JwtBearerEvents
                {
                    OnMessageReceived = context =>
                    {
                        var accessToken = context.Request.Query["access_token"];
                        var path = context.HttpContext.Request.Path;

                        if (!string.IsNullOrEmpty(accessToken) &&
                            path.StartsWithSegments("/api/hubs/user"))
                        {
                            context.Token = accessToken;
                        }

                        return Task.CompletedTask;
                    }
                };

                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidIssuer = keycloakSettings.HostName + "/realms/" + keycloakSettings.Realm,

                    ValidateAudience = true,
                    ValidAudience = keycloakSettings.FrontEndClientId,

                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true
                };
            });

            services.AddScoped<IClaimsTransformation, KeycloakClaimTransformer>();

            services.AddScoped<ICleanupService, CleanupService>();

            services.AddMediatR
                (
                cfg => cfg.RegisterServicesFromAssembly(typeof(AddUserToRolesCaseHandler).Assembly)
                );

            services.AddValidatorsFromAssembly(typeof(CreateProblemDeletionRequestValidator).Assembly);

            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));

            return services;
        }
    }
}
