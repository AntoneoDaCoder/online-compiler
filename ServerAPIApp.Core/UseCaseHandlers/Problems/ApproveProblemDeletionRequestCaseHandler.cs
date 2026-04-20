using Hangfire;
using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.UseCases.Problems;
using ServerAPIApp.Domain.Exceptions.NotFoundExceptions;
using ServerAPIApp.Domain.Constants;
using ServerAPIApp.Core.Helpers;
using ServerAPIApp.Core.Abstractions;

namespace ServerAPIApp.Core.UseCaseHandlers.Problems
{
    public class ApproveProblemDeletionRequestCaseHandler : IRequestHandler<ApproveProblemDeletionRequestCase>
    {
        private readonly IProblemDeletionRequestRepository _requestsRepo;
        private readonly IProblemRepository _problemRepo;

        public ApproveProblemDeletionRequestCaseHandler(IProblemDeletionRequestRepository requestsRepo, IProblemRepository problemRepo)
        {
            _requestsRepo = requestsRepo;
            _problemRepo = problemRepo;
        }

        public async Task Handle(ApproveProblemDeletionRequestCase command, CancellationToken cancellationToken = default)
        {
            var request = (await _requestsRepo.GetFilteredAsync(x => x.Id == command.RequestId, cancellationToken, includes: x => x.Problem)).FirstOrDefault();

            if (request is null)
                throw new ResourceNotFoundException("Deletion request not found.");

            var problem = request.Problem;

            var deletionInitiationDate = DateTimeOffset.UtcNow;
            var deletionDate = deletionInitiationDate.Add(ApplicationConstants.GracePeriod);

            problem.DeletionScheduledAt = deletionInitiationDate;
            problem.DeletionDeadline = deletionDate;
            problem.IsDeleted = true;

            string newJobId = BackgroundJob.Schedule<IProblemRepository>(
                svc => svc.DeleteAsync(problem),
                ApplicationConstants.GracePeriod);

            BackgroundJob.ContinueJobWith<ICleanupService>(newJobId,
                svc => svc.DeleteProblemRelatedMetadataAsync(request.Id, newJobId),
                JobContinuationOptions.OnlyOnSucceededState);

            problem.DeletionJobId = newJobId;

            await _problemRepo.UpdateAsync(problem, cancellationToken);
        }
    }
}
