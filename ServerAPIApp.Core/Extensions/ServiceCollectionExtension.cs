using k8s;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using ServerAPIApp.Core.Abstractions;
using ServerAPIApp.Core.Configs;
using ServerAPIApp.Core.Repositories;
using ServerAPIApp.Core.Services;

namespace ServerAPIApp.Core.Extensions
{
    public static class ServiceCollectionExtension
    {
        public static IServiceCollection BindLanguageConfigs(this IServiceCollection services, IConfiguration conf)
        {
            services.Configure<LanguageConfig>("csharp", conf.GetSection("Languages:csharp"));

            services.Configure<LanguageConfig>("swift", conf.GetSection("Languages:swift"));

            services.Configure<LanguageConfig>("java", conf.GetSection("Languages:java"));

            services.Configure<LanguageConfig>("postgresql", conf.GetSection("Languages:postgresql"));

            services.Configure<LanguageConfig>("nodejs", conf.GetSection("Languages:nodejs"));

            services.Configure<LanguageConfig>("typescript", conf.GetSection("Languages:typescript"));

            return services;
        }

        public static IServiceCollection RegisterServices(this IServiceCollection services)
        {
            services.AddSingleton<ProblemRepository>();

            services.AddSingleton<IKubernetes>(sp =>
            {
                var config = KubernetesClientConfiguration.BuildDefaultConfig();
                return new Kubernetes(config);
            });

            services.AddSingleton<CallbackService>();

            services.AddSingleton<IKubernetesJobManager>
                (sp =>
                {
                    var monitor = sp.GetRequiredService<IOptionsMonitor<LanguageConfig>>();

                    return new KubernetesJobManager
                    (
                        "csharp",
                        sp.GetRequiredService<IKubernetes>(),
                        monitor,
                        sp.GetRequiredService<CallbackService>()
                    );
                }
            );

            services.AddSingleton<IKubernetesJobManager>

               (sp =>
               {
                   var monitor = sp.GetRequiredService<IOptionsMonitor<LanguageConfig>>();

                   return new KubernetesJobManager
                   (
                       "swift",
                       sp.GetRequiredService<IKubernetes>(),
                       monitor,
                       sp.GetRequiredService<CallbackService>()
                   );
               }
           );

            services.AddSingleton<IKubernetesJobManager>
                (sp =>
                  {
                      var monitor = sp.GetRequiredService<IOptionsMonitor<LanguageConfig>>();

                      return new KubernetesJobManager
                      (
                          "java",
                          sp.GetRequiredService<IKubernetes>(),
                          monitor,
                          sp.GetRequiredService<CallbackService>()
                      );
                  }
              );

            services.AddSingleton<IKubernetesJobManager>
                (sp =>
                {
                    var monitor = sp.GetRequiredService<IOptionsMonitor<LanguageConfig>>();

                    return new KubernetesJobManager
                    (
                        "postgresql",
                        sp.GetRequiredService<IKubernetes>(),
                        monitor,
                        sp.GetRequiredService<CallbackService>()
                    );
                }
            );

            services.AddSingleton<IKubernetesJobManager>
            (sp =>
            {
                var monitor = sp.GetRequiredService<IOptionsMonitor<LanguageConfig>>();

                return new KubernetesJobManager
                (
                    "nodejs",
                    sp.GetRequiredService<IKubernetes>(),
                    monitor,
                    sp.GetRequiredService<CallbackService>()
                );
            }
            );

            services.AddSingleton<IKubernetesJobManager>
            (sp =>
            {
                var monitor = sp.GetRequiredService<IOptionsMonitor<LanguageConfig>>();
                return new KubernetesJobManager
                (
                    "typescript",
                    sp.GetRequiredService<IKubernetes>(),
                    monitor,
                    sp.GetRequiredService<CallbackService>()
                );
            }
            );

            services.AddHostedService<ManagerAdapter>();

            services.AddSingleton<ICodeDispatcher, CodeDispatcher>();
            services.AddHostedService(provider => provider.GetRequiredService<ICodeDispatcher>());


            return services;
        }
    }
}
