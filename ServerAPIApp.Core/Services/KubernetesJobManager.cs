using k8s;
using k8s.Models;
using Microsoft.Extensions.Hosting;
using Shared.DTOs;
using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;

namespace ServerAPIApp.Core.Services
{
    public class KubernetesJobManager : IHostedService, IDisposable
    {
        private const int _podMemoryLimitMb = 96;
        private const int _memoryBudgetMb = 1024;
        private const string _namespace = "default";
        private const string _imageName = "csharp-runner:local";
        private const string _containerName = "runner";
        private const string _deploymentName = "runners-deployment";
        private const int _numReplicas = _memoryBudgetMb / _podMemoryLimitMb;

        private ConcurrentDictionary<Guid, (string Name, string CallbackUrl)> _resultCallbacks =
            new ConcurrentDictionary<Guid, (string Name, string CallbackUrl)>();
        private IKubernetes _client;
        private CallbackService _callbackService;
        private CancellationTokenSource? _cts;
        private HttpClient _httpClient;
        private bool _isDisposed;

        public KubernetesJobManager(IKubernetes client, CallbackService callbackService)
        {
            _client = client;
            _callbackService = callbackService;
            _httpClient = new HttpClient();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            await EnsureResourceQuotaExistsAsync(_cts.Token);

            await EnsureDeploymentExistsAsync(_cts.Token);
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _cts?.Cancel();

            _cts?.Dispose();

            _cts = null;

            return Task.CompletedTask;
        }

        //can be done automatically via kubernetes service + readinessprobe, but i want to try and balance the load manually
        public async Task<bool> ExecuteAsync(CodeRequestDto request, CancellationToken token)
        {
            var pods = await _client.CoreV1.ListNamespacedPodAsync(
                             namespaceParameter: _namespace,
                             labelSelector: "app=runner,readyForExecution=yes",
                             cancellationToken: token);

            if (pods.Items.Count == 0)
                return false;

            var targetPod = pods.Items.First();

            var podName = targetPod.Metadata.Name;

            var podIp = targetPod.Status.PodIP;

            Console.WriteLine($"[KubernetesJobManager] Trying to execute request [Id:{request.RequestId} in pod [Name:{podName}]");

            try
            {
                var response = await _httpClient.PostAsJsonAsync($"http://{podIp}:5000/run", request, token);

                response.EnsureSuccessStatusCode();

                var patch = new V1Patch
                (
                    new
                    {
                        Metadata = new
                        {
                            Labels = new Dictionary<string, string>
                            {
                                ["app"] = "runner",
                                ["readyForExecution"] = "no"
                            }
                        },
                    },
                    V1Patch.PatchType.MergePatch
                );

                await _client.CoreV1.PatchNamespacedPodAsync(
                       body: patch,
                       name: podName,
                       namespaceParameter: _namespace,
                       cancellationToken: token);

                Console.WriteLine($"[KubernetesJobManager] Successfully assigned request [Id:{request.RequestId}] to pod [Name:{podName}]");

                _resultCallbacks.TryAdd(request.RequestId, (podName, request.CallbackUrl));

                Console.WriteLine($"[KubernetesJobManager] Current state of pending callbacks:\r\n "
                    + JsonSerializer.Serialize(_resultCallbacks, new JsonSerializerOptions { WriteIndented = true }));

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KubernetesJobManager] Error sending request to {podName}: {ex}");

                return false;
            }
        }

        public async Task CompleteJobAsync(CodeResponseDto podResponse, CancellationToken cancellationToken)
        {
            try
            {
                if (!_resultCallbacks.TryGetValue(podResponse.RequestId, out var requestData))
                {
                    throw new InvalidOperationException($"Job [Id:{podResponse.RequestId}] doesn't exist");
                }

                await _callbackService.NotifyClientAsync(podResponse, requestData.CallbackUrl, cancellationToken);

                var patch = new V1Patch
                (
                    new
                    {
                        Metadata = new
                        {
                            Labels = new Dictionary<string, string>
                            {
                                ["app"] = "runner",
                                ["readyForExecution"] = "yes"
                            }
                        },
                    },
                    V1Patch.PatchType.MergePatch
                );

                await _client.CoreV1.PatchNamespacedPodAsync(
                       body: patch,
                       name: requestData.Name,
                       namespaceParameter: _namespace,
                       cancellationToken: cancellationToken);

                _resultCallbacks.TryRemove(podResponse.RequestId, out _);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KubernetesJobManager] Failed to complete job [Id:{podResponse.RequestId}], reason: {ex}");
            }
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }

        protected virtual void Dispose(bool disposing)
        {
            if (_isDisposed)
            {
                return;
            }
            if (disposing)
            {
                _cts?.Cancel();

                _cts?.Dispose();
                _cts = null;

                _client.Dispose();

                _httpClient.Dispose();
            }
            _isDisposed = true;
        }

        private async Task EnsureResourceQuotaExistsAsync(CancellationToken token)
        {
            try
            {
                await _client.CoreV1.ReadNamespacedResourceQuotaAsync("runner-quota", _namespace, cancellationToken: token);
            }
            catch
            {
                var quota = new V1ResourceQuota
                {
                    Metadata = new V1ObjectMeta { Name = "runner-quota", NamespaceProperty = _namespace },
                    Spec = new V1ResourceQuotaSpec
                    {
                        Hard = new Dictionary<string, ResourceQuantity>()
                        {
                            ["limits.memory"] = new ResourceQuantity($"{_memoryBudgetMb}Mi"),
                            ["requests.memory"] = new ResourceQuantity($"{_memoryBudgetMb}Mi")
                        }
                    }
                };
                await _client.CoreV1.CreateNamespacedResourceQuotaAsync(
                      body: quota,
                      namespaceParameter: _namespace,
                      cancellationToken: token);
            }
        }

        private async Task EnsureDeploymentExistsAsync(CancellationToken token)
        {
            try
            {
                await _client.AppsV1.ReadNamespacedDeploymentAsync(
                       name: _deploymentName,
                       namespaceParameter: _namespace,
                       cancellationToken: token);
            }
            catch
            {
                var deployment = new V1Deployment
                {
                    Metadata = new V1ObjectMeta { Name = _deploymentName, NamespaceProperty = _namespace },
                    Spec = new V1DeploymentSpec
                    {
                        Replicas = _numReplicas,
                        Selector = new V1LabelSelector()
                        {
                            MatchLabels = new Dictionary<string, string>()
                            {
                                { "app", "runner" },
                                { "readyForExecution", "yes" +
                                "" }
                            }
                        },
                        Template = new V1PodTemplateSpec
                        {
                            Metadata = new V1ObjectMeta
                            {
                                Labels = new Dictionary<string, string>()
                                {
                                    { "app", "runner" },
                                    { "readyForExecution", "yes" }
                                }
                            },
                            Spec = new V1PodSpec
                            {
                                Containers = new List<V1Container>()
                                {
                                    new V1Container()
                                    {
                                        Name = _containerName,
                                        Image = _imageName,
                                        Resources = new V1ResourceRequirements
                                        {
                                            Limits = new Dictionary<string, ResourceQuantity>
                                            {
                                                ["memory"] = new ResourceQuantity($"{_podMemoryLimitMb}Mi")
                                            },
                                            Requests = new Dictionary<string, ResourceQuantity>
                                            {
                                                ["memory"] = new ResourceQuantity($"{_podMemoryLimitMb}Mi")
                                            }
                                        },
                                        SecurityContext = new V1SecurityContext
                                        {
                                            RunAsNonRoot = true,
                                            ReadOnlyRootFilesystem = false,
                                            AllowPrivilegeEscalation = false
                                        }
                                    }
                                }
                            }
                        }
                    }
                };

                await _client.AppsV1.CreateNamespacedDeploymentAsync(
                        body: deployment,
                        namespaceParameter: _namespace,
                        cancellationToken: token);
            }
        }
    }
}
