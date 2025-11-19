using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.UseCases.Problems;
using ServerAPIApp.Domain.Entities;
using ServerAPIApp.Core.Helpers;

namespace ServerAPIApp.Core.UseCaseHandlers.Problems
{
    public class CreateCaseHandler : IRequestHandler<CreateProblemCase, ProblemEntity>
    {
        private IProblemRepository _repo;

        public CreateCaseHandler(IProblemRepository repo)
        {
            _repo = repo;
        }

        public async Task<ProblemEntity> Handle(CreateProblemCase command, CancellationToken cancellationToken)
        {
            var entity = command.ToEntity();

            return await _repo.CreateAsync(entity, cancellationToken);
        }
    }
}
