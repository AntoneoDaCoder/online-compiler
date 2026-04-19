using Hangfire;
using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Contracts.DTOs.Problems;
using ServerAPIApp.Core.UseCases.Problems;
using ServerAPIApp.Domain.Exceptions.NotFoundExceptions;

namespace ServerAPIApp.Core.UseCaseHandlers.Problems
{
    public class RestoreCaseHandler : IRequestHandler<RestoreProblemCase, ProblemDto>
    {
        private IProblemRepository _repo;

        public RestoreCaseHandler(IProblemRepository repo)
        {
            _repo = repo;
        }

        public async Task<ProblemDto> Handle(RestoreProblemCase command, CancellationToken cancellationToken)
        {
            var entity = (await _repo.GetFilteredAsync(x => x.Id == command.ProblemId, cancellationToken)).FirstOrDefault();

            if (entity is null)
                throw new ResourceNotFoundException("Problem not found.");

            if (entity.DeletionJobId is not null)
                BackgroundJob.Delete(entity.DeletionJobId);

            entity.DeletionJobId = null;
            entity.DeletionDeadline = null;
            entity.DeletionScheduledAt = null;
            entity.IsDeleted = false;

            var res = await _repo.UpdateAsync(entity, cancellationToken);

            return ProblemDto.From(res!);
        }
    }
}
