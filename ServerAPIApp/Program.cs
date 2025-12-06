using ServerAPIApp.Core.Extensions;
using ServerAPIApp.Extensions;
using ServerAPIApp.Hubs;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:8080");

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

var app = builder.Build();

app.UseHttpsRedirection();

app.UseCors();

app.ConfigureMiddleware();

app.MapControllers();

app.MapHub<ResultHub>("/hubs/result");

app.Run();
