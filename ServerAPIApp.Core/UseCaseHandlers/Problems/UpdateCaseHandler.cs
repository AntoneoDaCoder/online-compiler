using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.Helpers;
using ServerAPIApp.Core.UseCases.Problems;
using ServerAPIApp.Domain.Entities;
using ServerAPIApp.Domain.Exceptions;

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
            var updated = command.ToEntity();

            var entity = await _repo.UpdateAsync(updated, cancellationToken);

            return entity;
        }
    }
}
