using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.UseCases.Problems;
using ServerAPIApp.Domain.Entities;
using ServerAPIApp.Core.Helpers;
using ServerAPIApp.Domain.Exceptions;
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

            if (!string.IsNullOrEmpty(entity.Slug))
                throw new EmptyFieldException("Slug cannot be empty");

            if (!string.IsNullOrEmpty(entity.Title))
                throw new EmptyFieldException("Title cannot be empty");

            var res = await _repo.CreateAsync(entity, cancellationToken);

            return ProblemDto.From(res);
        }
    }
}
