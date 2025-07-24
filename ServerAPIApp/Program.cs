using ServerAPIApp.Core.Extensions;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:8080");

builder.Services.AddControllers();

builder.Services.BindLanguageConfigs(builder.Configuration);

builder.Services.RegisterServices();

var app = builder.Build();

app.MapControllers();

app.Run();
