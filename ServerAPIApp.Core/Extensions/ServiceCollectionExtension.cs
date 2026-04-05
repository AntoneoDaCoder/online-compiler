using FluentValidation;
using MediatR;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
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
                  options.Authority = "http://keycloak-server:8081/realms/clinic-app-realm";
                  //options.Audience = keycloakSettings.ClientId;
                  options.Audience = "account"; //TODO: CHANGE IT LATER 
                  options.RequireHttpsMetadata = false;
                  options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters()
                  {
                      ValidateIssuer = true,
                      ValidateAudience = true,
                      ValidateLifetime = true,
                      ValidateIssuerSigningKey = true,
                      ValidIssuer = "http://keycloak-server:8081/realms/clinic-app-realm",
                      ValidAudience = "account",
                  };

                  options.Events = new JwtBearerEvents
                  {
                      OnMessageReceived = context =>
                      {
                          string? accessToken = null;

                          if (context.Request.Cookies.TryGetValue("access_token", out var cookieToken) && !string.IsNullOrEmpty(cookieToken))
                          {
                              accessToken = cookieToken;
                          }
                          else
                          {
                              var header = context.Request.Headers["Authorization"].FirstOrDefault();
                              if (!string.IsNullOrEmpty(header))
                              {
                                  accessToken = header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
                                      ? header.Substring("Bearer ".Length).Trim()
                                      : header.Trim();
                              }
                          }

                          context.Token = accessToken;

                          return Task.CompletedTask;
                      },
                      OnAuthenticationFailed = ctx =>
                      {
                          var logger = ctx.HttpContext.RequestServices.GetRequiredService<ILoggerFactory>().CreateLogger("JwtAuth");
                          logger.LogError(ctx.Exception, "OnAuthenticationFailed");
                          return Task.CompletedTask;
                      }
                  };
              }
              );

            services.AddScoped<IClaimsTransformation, KeycloakClaimTransformer>();

            var keycloakConf = config.GetSection("KeycloakConfiguration");
            services.Configure<KeycloakConfiguration>(keycloakConf);
            var keycloakSettings = keycloakConf.Get<KeycloakConfiguration>();

            services.AddHttpClient<IExternalAuthService, KeycloakService>((sp, client) =>
            {
                client.BaseAddress = new Uri(keycloakSettings.BaseUrl);
            });


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
