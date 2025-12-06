using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.UseCases.Problems;
using ServerAPIApp.Domain.Entities;
using ServerAPIApp.Domain.Exceptions.NotFoundExceptions;

namespace ServerAPIApp.Core.UseCaseHandlers.Problems
{
    public class GetByIdCaseHandler : IRequestHandler<GetProblemByIdCase, ProblemEntity>
    {
        private IProblemRepository _repo;

        public GetByIdCaseHandler(IProblemRepository repo)
        {
            _repo = repo;
        }

        public async Task<ProblemEntity> Handle(GetProblemByIdCase command, CancellationToken cancellationToken)
        {
            var entity = await _repo.GetByIdAsync(command.Id, cancellationToken);

            if (entity is null)
                throw new ResourceNotFoundException("Resource not found");

            return entity;
        }
    }
}
