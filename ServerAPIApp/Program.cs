using ServerAPIApp.Core.Extensions;
using ServerAPIApp.Hubs;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:8080");

builder.Services.AddControllers();

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
