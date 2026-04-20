using Hangfire;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.Abstractions;
using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Core.Services
{
    public class CleanupService : ICleanupService
    {
        private readonly IProblemDeletionRequestRepository _repo;

        public CleanupService(IProblemDeletionRequestRepository repo)
        {
            _repo = repo;
        }

        public async Task DeleteProblemRelatedMetadataAsync(Guid requestId, string jobId, CancellationToken cancellationToken = default)
        {
            var stub = new ProblemDeletionRequestEntity()
            {
                Id = requestId
            };

            await _repo.DeleteAsync(stub, cancellationToken);

            BackgroundJob.Delete(jobId);
        }
    }
}
