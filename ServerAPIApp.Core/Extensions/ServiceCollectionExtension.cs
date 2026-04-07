using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using ServerAPIApp.Core.Abstractions;
using ServerAPIApp.Core.AuthorizationRequirements;
using ServerAPIApp.Core.Configs;
using ServerAPIApp.Core.PipelineBehaviours;
using ServerAPIApp.Core.Services;
using ServerAPIApp.Core.UseCaseHandlers.Users;
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
            .AddJwtBearer
              (
              options =>
              {
                  options.Authority = keycloakSettings.BaseUrl + "/realms/" + keycloakSettings.Realm;
                  options.Audience = keycloakSettings.FrontEndClientId;
                  options.RequireHttpsMetadata = false;
                  options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
                  {
                      ValidateIssuer = true,
                      ValidateAudience = true,
                      ValidateLifetime = true,
                      ValidateIssuerSigningKey = true,
                      ValidIssuer = keycloakSettings.BaseUrl + "/realms/" + keycloakSettings.Realm,
                      ValidAudience = keycloakSettings.FrontEndClientId
                  };
              }
              );

            services.AddScoped<IClaimsTransformation, KeycloakClaimTransformer>();

            services.AddMediatR
                (
                cfg => cfg.RegisterServicesFromAssembly(typeof(AddUserToRolesCaseHandler).Assembly)
                );

            //services.AddValidatorsFromAssembly(typeof(RegisterUserValidator).Assembly);

            services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehaviour<,>));

            return services;
        }
    }
}
