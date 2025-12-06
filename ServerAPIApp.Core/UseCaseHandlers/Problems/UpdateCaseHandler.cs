using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.Helpers;
using ServerAPIApp.Core.UseCases.Problems;
using ServerAPIApp.Domain.Entities;
using ServerAPIApp.Domain.Exceptions.NotFoundExceptions;

namespace ServerAPIApp.Core.UseCaseHandlers.Problems
{
    public class UpdateCaseHandler : IRequestHandler<UpdateProblemCase,ProblemEntity>
    {
        private IProblemRepository _repo;
        public UpdateCaseHandler(IProblemRepository repo)
        {
            _repo = repo;
        }

        public async Task<ProblemEntity> Handle(UpdateProblemCase command, CancellationToken cancellationToken)
        {
            var entity = command.ToEntity();

            var updated = await _repo.UpdateAsync(entity, cancellationToken);

            if (!updated)
                throw new ResourceNotFoundException("Resource not found");

            return entity;
        }
    }
}
