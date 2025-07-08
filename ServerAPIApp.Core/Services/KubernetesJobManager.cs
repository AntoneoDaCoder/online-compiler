using k8s;
using k8s.Models;
using Microsoft.Extensions.Hosting;
using ServerAPIApp.Core.DTOs;
using ServerAPIApp.Core.Enums;
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
        private const int _pollIntervalMs = 500;
        private const int _numReplicas = _memoryBudgetMb / _podMemoryLimitMb;

        private ConcurrentDictionary<Guid, CodeRequestDto> _resultCallbacks = new ConcurrentDictionary<Guid, CodeRequestDto>();
        private IKubernetes _client;
        private CallbackService _callbackService;
        private CancellationTokenSource? _cts;
        private Task? _pollingTask;
        private HttpClient _httpClient;
        private bool _isDisposed;
        private bool _isPolling;

        public KubernetesJobManager(IKubernetes client, CallbackService callbackService)
        {
            _client = client;
            _callbackService = callbackService;
            _httpClient = new HttpClient();
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (_isPolling)
            {
                return;
            }
            _isPolling = true;

            _cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);

            await EnsureResourceQuotaExistsAsync(_cts.Token);

            await EnsureDeploymentExistsAsync(_cts.Token);

            _pollingTask = Task.Run(() => PollKubernetesAsync(_cts.Token), _cts.Token);
        }

        public async Task StopAsync(CancellationToken cancellationToken)
        {
            if (!_isPolling)
            {
                return;
            }

            _isPolling = false;

            _cts?.Cancel();

            if (_pollingTask is not null)
                await _pollingTask;

            _cts?.Dispose();
            _pollingTask = null;
            _cts = null;
        }

        //public async Task<bool> ExecuteAsync(CodeRequestDto request, CancellationToken token)
        //{
        //    var jobName = request.RequestId.ToString();

        //    var job = new V1Job
        //    {
        //        Metadata = new V1ObjectMeta(name: jobName, namespaceProperty: _namespace),
        //        Spec = new V1JobSpec
        //        {
        //            ActiveDeadlineSeconds = 60,
        //            Template = new V1PodTemplateSpec
        //            {
        //                Spec = new V1PodSpec
        //                {
        //                    RestartPolicy = "Never",
        //                    Containers = new List<V1Container>
        //                    {
        //                        new V1Container
        //                        {
        //                            Name = _containerName,
        //                            Image = _imageName,
        //                            Env = new List<V1EnvVar>
        //                            {
        //                                new V1EnvVar { Name = "USER_CODE", Value = request.Code },
        //                                new V1EnvVar { Name = "EXECUTION_TIMEOUT", Value = request.MaxAllowedTimeInMilliseconds.ToString() }
        //                            },
        //                    VolumeMounts = new List<V1VolumeMount>
        //                    {
        //                        new V1VolumeMount
        //                        {
        //                            Name = "app-volume",
        //                            MountPath = "/workspace"
        //                        }
        //                    },
        //                    WorkingDir = "/workspace",
        //                    Resources = new V1ResourceRequirements
        //                    {
        //                        Limits = new Dictionary<string, ResourceQuantity>
        //                        {
        //                            ["memory"] = new ResourceQuantity($"{_jobMemoryLimitMb}Mi")
        //                        },
        //                        Requests = new Dictionary<string, ResourceQuantity>
        //                        {
        //                            ["memory"] = new ResourceQuantity($"{_jobMemoryLimitMb}Mi")
        //                        }
        //                    },
        //                    SecurityContext = new V1SecurityContext
        //                    {
        //                        RunAsNonRoot = true,
        //                        ReadOnlyRootFilesystem = false,
        //                        AllowPrivilegeEscalation = false
        //                    }
        //                }
        //        },
        //                    Volumes = new List<V1Volume>
        //        {
        //            new V1Volume
        //            {
        //                Name = "app-volume",
        //                EmptyDir = new V1EmptyDirVolumeSource()
        //            }
        //        }
        //                }
        //            }
        //        }
        //    };

        //    try
        //    {
        //        await _client.BatchV1.CreateNamespacedJobAsync(job, _namespace, cancellationToken: token);

        //        _resultCallbacks.TryAdd(request.RequestId, request);

        //        return true;
        //    }
        //    catch (Exception ex)
        //    {
        //        Console.WriteLine("[KubernetesJobManager] Failed to schedule execution. Reason: " + ex.Message);

        //        return false;
        //    }
        //}

        //can be done automatically via kubernetes service + readinessprobe, but i want to try and balance the load manually
        public async Task<bool> ExecuteAsync(CodeRequestDto request, CancellationToken token)
        {
            var pods = await _client.CoreV1.ListNamespacedPodAsync(
                             namespaceParameter: _namespace,
                             labelSelector: "app=runner,status=free",
                             cancellationToken: token);

            if (pods.Items.Count == 0)
                return false;

            var targetPod = pods.Items.First();

            var podName = targetPod.Metadata.Name;

            var podIp = targetPod.Status.PodIP;

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
                                ["status"] = "busy"
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

                return true;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[KubernetesJobManager] Error sending request to {podName}: {ex.Message}; \r\n{ex.StackTrace}");
                return false;
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

                if (_pollingTask is not null)
                {
                    try
                    {
                        _pollingTask.GetAwaiter().GetResult();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"[KubernetesJobManager] Error while disposing polling task: {ex}");
                    }
                }

                _cts?.Dispose();
                _cts = null;
                _pollingTask = null;
                _client.Dispose();
                _isPolling = false;
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
                                { "status", "free" }
                            }
                        },
                        Template = new V1PodTemplateSpec
                        {
                            Metadata = new V1ObjectMeta
                            {
                                Labels = new Dictionary<string, string>()
                                {
                                    { "app", "runner" },
                                    { "status", "free" }
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

        private async Task PollKubernetesAsync(CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                var jobs = await _client.BatchV1.ListNamespacedJobAsync(_namespace, cancellationToken: token);

                foreach (var job in jobs)
                {
                    var isSucceeded = job.Status.Succeeded.HasValue && job.Status.Succeeded.Value > 0;
                    var isFailed = job.Status.Failed.HasValue && job.Status.Failed.Value > 0;

                    if (!isSucceeded && !isFailed)
                        continue;

                    var pods = await _client.CoreV1.ListNamespacedPodAsync(
                        _namespace,
                        labelSelector: $"job-name={job.Metadata.Name}",
                        cancellationToken: token);

                    var pod = pods.Items.FirstOrDefault();
                    if (pod == null) continue;

                    var containerStatus = pod.Status.ContainerStatuses?.FirstOrDefault();
                    var state = containerStatus?.State?.Terminated;

                    string reason = state.Reason ?? "Unknown";
                    int exitCode = state.ExitCode;

                    if (!_resultCallbacks.TryGetValue(Guid.Parse(job.Metadata.Name), out var requestDto))
                    {
                        await _client.BatchV1.DeleteNamespacedJobAsync(
                            name: job.Metadata.Name,
                            namespaceParameter: _namespace,
                            new V1DeleteOptions(),
                            propagationPolicy: "Foreground",
                            cancellationToken: token);

                        continue;
                    }

                    var resultDto = new CodeResponseDto()
                    {
                        RequestId = requestDto.RequestId,
                        Result = new ExecutionResultDto()
                        {
                            RequestSentAt = requestDto.RequestSentAt,
                            ExitCode = exitCode,
                        }
                    };

                    if (isFailed)
                    {
                        resultDto.Status = RequestStatus.Failed;

                        if (exitCode == 124)
                        {
                            resultDto.Result.Status = ExecutionStatus.TimedOut;
                        }
                        else
                        {
                            using (var logStream = await _client.CoreV1.ReadNamespacedPodLogAsync(
                                            name: pod.Metadata.Name,
                                            namespaceParameter: _namespace,
                                            container: _containerName,
                                            cancellationToken: token))
                            {
                                using var reader = new StreamReader(logStream);
                                string logs = await reader.ReadToEndAsync(token);

                                if (logs.Contains("[Compile error]"))
                                {
                                    resultDto.Result.Status = ExecutionStatus.CompileError;
                                }
                                else
                                {
                                    resultDto.Result.Status = ExecutionStatus.RuntimeError;
                                }

                                resultDto.Result.ConsoleOutput = logs;
                            }
                        }
                    }
                    else
                    {
                        resultDto.Status = RequestStatus.Succeeded;
                        resultDto.Result.Status = ExecutionStatus.Succeded;
                    }

                    await _client.BatchV1.DeleteNamespacedJobAsync(
                            name: job.Metadata.Name,
                            namespaceParameter: _namespace,
                            new V1DeleteOptions(),
                            propagationPolicy: "Foreground",
                            cancellationToken: token);

                    await _callbackService.NotifyClientAsync(resultDto, requestDto.CallbackUrl, token);

                    _resultCallbacks.TryRemove(requestDto.RequestId, out _);

                }

                await Task.Delay(_pollIntervalMs, token);
            }
        }
    }
}
