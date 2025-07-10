using k8s;
using ServerAPIApp.Core.Services;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://0.0.0.0:8080");

builder.Services.AddControllers();


builder.Services.AddSingleton<IKubernetes>(sp =>
{
    var config = KubernetesClientConfiguration.BuildDefaultConfig();
    return new Kubernetes(config);
});

builder.Services.AddSingleton<CallbackService>();

builder.Services.AddSingleton<KubernetesJobManager>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<KubernetesJobManager>());

builder.Services.AddSingleton<CodeDispatcher>();
builder.Services.AddHostedService(provider => provider.GetRequiredService<CodeDispatcher>());

var app = builder.Build();


app.MapControllers();

app.Run();
