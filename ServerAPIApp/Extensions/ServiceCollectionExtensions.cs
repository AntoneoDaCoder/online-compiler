using k8s;
using Microsoft.Extensions.Options;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Configs;
using ServerAPIApp.Dispatchers;
using ServerAPIApp.Notifiers;

namespace ServerAPIApp.Extensions
{
    public static class ServiceCollectionExtensions
    {
        private static readonly string[] SupportedLanguages = new[]
        {
            "csharp", /*"swift",*/ "java", "postgresql",
            "mssql", "nodejs", "kotlin", "typescript"
        };

        public static void ConfigureDispatchers(this IServiceCollection services, IConfiguration config)
        {
            services.AddSignalR();

            services.AddScoped<ISubmissionNotifier, SubmissionNotifier>();

            services.AddSingleton<IKubernetes>(sp =>
            {
                var kubeConfig = KubernetesClientConfiguration.BuildDefaultConfig();
                return new Kubernetes(kubeConfig);
            });

            var useComposite = config.GetValue<bool>("UseComposite");

            if (useComposite)
            {
                Console.WriteLine("[API] Server starts in composite mode");

                services.Configure<LanguageConfig>("composite", config.GetSection($"Languages:composite"));

                services.AddSingleton<CompositeKubernetesJobManager>(sp =>
                {
                    var mgr = new CompositeKubernetesJobManager(
                        sp.GetRequiredService<IKubernetes>(),
                        sp.GetRequiredService<IOptionsMonitor<LanguageConfig>>());

                    foreach (var lang in SupportedLanguages)
                        mgr.RegisterLanguage(lang);

                    return mgr;
                });

                foreach (var lang in SupportedLanguages)
                {
                    services.AddSingleton<IKubernetesJobManager>(sp =>
                        new CompositeJobManagerProxy(lang, sp.GetRequiredService<CompositeKubernetesJobManager>()));
                }
            }
            else
            {
                Console.WriteLine("[API] Server starts in default mode");

                foreach (var lang in SupportedLanguages)
                {
                    services.Configure<LanguageConfig>(lang, config.GetSection($"Languages:{lang}"));
                }

                foreach (var lang in SupportedLanguages)
                {
                    services.AddSingleton<IKubernetesJobManager>(sp =>
                    {
                        var monitor = sp.GetRequiredService<IOptionsMonitor<LanguageConfig>>();
                        return new KubernetesJobManager(
                            lang,
                            sp.GetRequiredService<IKubernetes>(),
                            monitor
                        );
                    });
                }
            }

            services.AddHostedService<ManagerAdapter>();
            services.AddSingleton<ICodeDispatcher, CodeDispatcher>();
            services.AddHostedService(provider => provider.GetRequiredService<ICodeDispatcher>());
        }
    }
}
