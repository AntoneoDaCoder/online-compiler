using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.UseCases.Problems;
using ServerAPIApp.Core.Helpers;
using ServerAPIApp.Domain.Exceptions.BadRequestExceptions;
using ServerAPIApp.Contracts.DTOs.Problems;

namespace ServerAPIApp.Core.UseCaseHandlers.Problems
{
    public class CreateCaseHandler : IRequestHandler<CreateProblemCase, ProblemDto>
    {
        private IProblemRepository _repo;

        public CreateCaseHandler(IProblemRepository repo)
        {
            _repo = repo;
        }

        public async Task<ProblemDto> Handle(CreateProblemCase command, CancellationToken cancellationToken)
        {
            var entity = command.ToEntity();

            var res = await _repo.CreateAsync(entity, cancellationToken);

            return ProblemDto.From(res);
        }
    }
}
