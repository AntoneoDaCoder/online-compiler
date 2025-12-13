using ServerAPIApp.Core.Extensions;
using ServerAPIApp.Extensions;
using ServerAPIApp.Hubs;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("https://0.0.0.0:8080");

builder.Services.AddControllers();

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.AddCors(options =>
{
    options.AddDefaultPolicy(policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

builder.Services.RegisterServices(builder.Configuration);

builder.Services.ConfigureDispatchers(builder.Configuration);

builder.Services.AddRoleHandler();

var app = builder.Build();

app.UseHttpsRedirection();

app.UseCors();

app.UseAuthentication();

app.UseAuthorization();

app.ConfigureMiddleware();

app.MapControllers();

app.MapHub<UserHub>("/hubs/user");

app.MapHub<AdminHub>("/hubs/admin");

app.Run();
