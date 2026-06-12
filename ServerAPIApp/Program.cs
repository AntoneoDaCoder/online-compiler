using ServerAPIApp.Core.Extensions;
using ServerAPIApp.Extensions;
using ServerAPIApp.Hubs;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:8080");

//builder.WebHost.ConfigureKestrel(serverOptions =>
//{
//    serverOptions.ConfigureHttpsDefaults(httpsOptions =>
//    {
//        // .NET 5+ поддерживает PEM
//        var cert = X509Certificate2.CreateFromPemFile(
//            "cert.crt",
//            "key.key"
//        );
//        httpsOptions.ServerCertificate = cert;
//    });
//});

builder.Services.AddCors(options =>
{
    options.AddPolicy("DevCors", policy =>
    {
        policy
            .WithOrigins("http://localhost:4200")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials()
            .SetPreflightMaxAge(TimeSpan.FromSeconds(5));
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });

builder.Services.RegisterServices(builder.Configuration);

builder.Services.ConfigureDispatchers(builder.Configuration);

builder.Services.AddRoleHandler();

var app = builder.Build();

app.MigrateDatabase();

//app.UseHttpsRedirection();

app.UseRouting();

app.UseCors("DevCors");

app.UseAuthentication();

app.UseAuthorization();

app.ConfigureMiddleware();

app.MapControllers();

app.MapHub<UserHub>("/api/hubs/user");

app.Run();
