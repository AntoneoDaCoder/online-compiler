using ServerAPIApp.Core.Extensions;
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


builder.Services.BindLanguageConfigs(builder.Configuration);

builder.Services.RegisterServices();

builder.Services.AddSignalR();

var app = builder.Build();

app.UseCors();

app.MapControllers();

app.MapHub<ResultHub>("/hubs/result");

app.Run();
