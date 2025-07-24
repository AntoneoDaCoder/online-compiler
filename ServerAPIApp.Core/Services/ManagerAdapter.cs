using Microsoft.Extensions.Hosting;
using ServerAPIApp.Core.Abstractions;

namespace ServerAPIApp.Core.Services
{
    public class ManagerAdapter : IHostedService
    {
        private IEnumerable<IKubernetesJobManager> _managers;
        public ManagerAdapter(IEnumerable<IKubernetesJobManager> managers)
        {
            _managers = managers;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            foreach (var mgr in _managers.OfType<IHostedService>())
            {
                await mgr.StartAsync(cancellationToken);
            }
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            foreach (var mgr in _managers.OfType<IHostedService>())
            {
                await mgr.StopAsync(cancellationToken);
            }
        }
    }
}
