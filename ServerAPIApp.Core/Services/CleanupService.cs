using Hangfire;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.Abstractions;
using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Core.Services
{
    public class CleanupService : ICleanupService
    {
        private readonly IProblemDeletionRequestRepository _repo;
        private readonly IExternalAuthService _svc;

        public CleanupService(IProblemDeletionRequestRepository repo, IExternalAuthService svc)
        {
            _repo = repo;
            _svc = svc;
        }

        public async Task DeleteProblemRelatedMetadataAsync(Guid requestId, string jobId, CancellationToken cancellationToken = default)
        {
            var stub = new ProblemDeletionRequestEntity()
            {
                Id = requestId
            };

            await _repo.DeleteAsync(stub, cancellationToken);

            DeleteCompletedJob(jobId);
        }

        public async Task DeleteUserRelatedMetadataAsync(string userId, string jobId, CancellationToken cancellationToken = default)
        {
            await _svc.DeleteAccountAsync(userId, cancellationToken);

            DeleteCompletedJob(jobId);
        }

        public void DeleteCompletedJob(string jobId)
        {
            BackgroundJob.Delete(jobId);
        }
    }
}
