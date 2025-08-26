using k8s;
using k8s.Models;
using Microsoft.Extensions.Options;
using ServerAPIApp.Core.Abstractions;
using ServerAPIApp.Core.Configs;
using Shared.DTOs;
using System.Collections.Concurrent;
using System.Net.Http.Json;
using System.Text.Json;

namespace ServerAPIApp.Core.Services
{
    public class KubernetesJobManager : IKubernetesJobManager
    {
        public string Language { get; }

        protected int _podMemoryLimitMb;
        protected int _memoryBudgetMb;
        protected int _numReplicas;

        protected string _imageName = string.Empty;
        protected string _deploymentName = string.Empty;
        protected string _containerName = string.Empty;
        protected string _namespace = string.Empty;
        protected string _appLabel = string.Empty;

        private ConcurrentDictionary<Guid, (string Name, string CallbackUrl)> _resultCallbacks =
            new ConcurrentDictionary<Guid, (string Name, string CallbackUrl)>();

        private IKubernetes _client;
        private CallbackService _callbackService;
        private CancellationTokenSource? _cts;
        private HttpClient _httpClient;
        private bool _isDisposed;

        public KubernetesJobManager(string language, IKubernetes client, IOptionsMonitor<LanguageConfig> config, CallbackService callbackService)
        {
            Language = language;
            _client = client;
            _callbackService = callbackService;
            _httpClient = new HttpClient();

            var section = config.Get(language);

            _memoryBudgetMb = section.MemoryBudgetMib;
            _podMemoryLimitMb = section.MemoryLimitMib;
            _numReplicas = _memoryBudgetMb / _podMemoryLimitMb;

            _imageName = section.ImageName;
            _deploymentName = language + "-runners-deployment";
            _containerName = language + "-runner";
            _namespace = language + "-runners-namespace";
            _appLabel = language + "-app-runner";
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            await EnsureNamespaceExistsAsync(_cts.Token);

            await EnsureNetworkPolicyExistsAsync(_cts.Token);

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
        public async Task<bool> ExecuteAsync(ProblemSolutionDto request, CancellationToken token)
        {
            var pods = await _client.CoreV1.ListNamespacedPodAsync(
                             namespaceParameter: _namespace,
                             labelSelector: $"app={_appLabel},readyForExecution=yes",
                             cancellationToken: token);

            Console.WriteLine($"[KubernetesJobManager] Found {pods.Items.Count} available runners, time:" + DateTime.UtcNow.ToString("o"));

            if (pods.Items.Count == 0)
                return false;

            var targetPod = pods.Items.First();

            var podName = targetPod.Metadata.Name;

            var podIp = targetPod.Status.PodIP;

            Console.WriteLine($"[KubernetesJobManager] Trying to execute request [Id:{request.RequestId}] in pod [Name:{podName}], time:" + DateTime.UtcNow.ToString("o"));

            try
            {
                _resultCallbacks.TryAdd(request.RequestId, (podName, request.CallbackUrl));

                Console.WriteLine($"[KubernetesJobManager] Current state of pending callbacks:\r\n "
                    + JsonSerializer.Serialize(_resultCallbacks, new JsonSerializerOptions { WriteIndented = true }) + $", time:" + DateTime.UtcNow.ToString("o"));

                var patch = new V1Patch
                (
                    new
                    {
                        Metadata = new
                        {
                            Labels = new Dictionary<string, string>
                            {
                                ["app"] = _appLabel,
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

                Console.WriteLine($"[KubernetesJobManager] Marked pod [Name:{podName} as busy");

                var response = await _httpClient.PostAsJsonAsync($"http://{podIp}:5000/run", request, token);

                response.EnsureSuccessStatusCode();

                Console.WriteLine($"[KubernetesJobManager] Successfully assigned request [Id:{request.RequestId}] to pod [Name:{podName}], time:" + DateTime.UtcNow.ToString("o"));

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KubernetesJobManager] Error sending request to {podName}: {ex}, time:" + DateTime.UtcNow.ToString("o"));

                return false;
            }
        }

        public async Task CompleteJobAsync(CodeResponseDto podResponse, CancellationToken cancellationToken)
        {
            try
            {
                if (!_resultCallbacks.TryGetValue(podResponse.RequestId, out var requestData))
                {
                    throw new InvalidOperationException($"Job [Id:{podResponse.RequestId}] doesn't exist, time:" + DateTime.UtcNow.ToString("o"));
                }

                _resultCallbacks.TryRemove(podResponse.RequestId, out _);

                var patch = new V1Patch
                (
                    new
                    {
                        Metadata = new
                        {
                            Labels = new Dictionary<string, string>
                            {
                                ["app"] = _appLabel,
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

                Console.WriteLine($"[KubernetesJobManager] Marked pod [Name:{requestData.Name} as free");

                await _callbackService.NotifyClientAsync(podResponse, requestData.CallbackUrl, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KubernetesJobManager] Failed to complete job [Id:{podResponse.RequestId}], reason: {ex}, time:" + DateTime.UtcNow.ToString("o"));
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

        private async Task EnsureNamespaceExistsAsync(CancellationToken token)
        {
            try
            {
                await _client.CoreV1.ReadNamespaceAsync(_namespace, cancellationToken: token);
            }
            catch (k8s.Autorest.HttpOperationException ex) when (ex.Response.StatusCode == System.Net.HttpStatusCode.NotFound)
            {
                var ns = new V1Namespace
                {
                    Metadata = new V1ObjectMeta { Name = _namespace }
                };

                await _client.CoreV1.CreateNamespaceAsync(ns, cancellationToken: token);
            }
        }

        private async Task EnsureNetworkPolicyExistsAsync(CancellationToken token)
        {
            try
            {
                await _client.NetworkingV1.ReadNamespacedNetworkPolicyAsync(
                    name: $"{Language}-runner-egress-policy",
                    namespaceParameter: _namespace,
                    cancellationToken: token);
            }
            catch
            {
                var policy = new V1NetworkPolicy
                {
                    Metadata = new V1ObjectMeta
                    {
                        Name = $"{Language}-runner-egress-policy",
                        NamespaceProperty = _namespace
                    },
                    Spec = new V1NetworkPolicySpec
                    {
                        PodSelector = new V1LabelSelector
                        {
                            MatchLabels = new Dictionary<string, string>
                    {
                        { "app", _appLabel }
                    }
                        },
                        PolicyTypes = new List<string> { "Egress" },
                        Egress = new List<V1NetworkPolicyEgressRule>
                {
                    new V1NetworkPolicyEgressRule
                    {
                        To = new List<V1NetworkPolicyPeer>
                        {
                            new V1NetworkPolicyPeer
                            {
                                NamespaceSelector = new V1LabelSelector
                                {
                                    MatchLabels = new Dictionary<string, string>
                                    {
                                        { "kubernetes.io/metadata.name", "kube-system" }
                                    }
                                },
                                PodSelector = new V1LabelSelector
                                {
                                    MatchLabels = new Dictionary<string, string>
                                    {
                                        { "k8s-app", "kube-dns" }
                                    }
                                }
                            }
                        },
                        Ports = new List<V1NetworkPolicyPort>
                        {
                            new V1NetworkPolicyPort { Port = 53, Protocol = "UDP" },
                            new V1NetworkPolicyPort { Port = 53, Protocol = "TCP" }
                        }
                    },
                    new V1NetworkPolicyEgressRule
                    {
                        To = new List<V1NetworkPolicyPeer>
                        {
                            new V1NetworkPolicyPeer
                            {
                                NamespaceSelector = new V1LabelSelector
                                {
                                    MatchLabels = new Dictionary<string, string>
                                    {
                                        { "kubernetes.io/metadata.name", "default" }
                                    }
                                },
                                PodSelector = new V1LabelSelector
                                {
                                    MatchLabels = new Dictionary<string, string>
                                    {
                                        { "app", "api-server" }
                                    }
                                }
                            }
                        },
                        Ports = new List<V1NetworkPolicyPort>
                        {
                            new V1NetworkPolicyPort { Port = 8080, Protocol = "TCP" }
                        }
                    }
                }
                    }
                };

                await _client.NetworkingV1.CreateNamespacedNetworkPolicyAsync(
                    body: policy,
                    namespaceParameter: _namespace,
                    cancellationToken: token);
            }
        }


        private async Task EnsureResourceQuotaExistsAsync(CancellationToken token)
        {
            try
            {
                await _client.CoreV1.ReadNamespacedResourceQuotaAsync($"{Language}-runner-quota", _namespace, cancellationToken: token);
            }
            catch
            {
                var quota = new V1ResourceQuota
                {
                    Metadata = new V1ObjectMeta { Name = $"{Language}-runner-quota", NamespaceProperty = _namespace },
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
                                { "app",  _appLabel },                            }
                        },
                        Template = new V1PodTemplateSpec
                        {
                            Metadata = new V1ObjectMeta
                            {
                                Labels = new Dictionary<string, string>()
                                {
                                     { "app",  _appLabel },
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
