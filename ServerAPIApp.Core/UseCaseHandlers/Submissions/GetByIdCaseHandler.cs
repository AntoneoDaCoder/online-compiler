using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Contracts.DTOs;
using ServerAPIApp.Core.UseCases.Submissions;
using ServerAPIApp.Domain.Entities;
using ServerAPIApp.Domain.Exceptions.NotFoundExceptions;

namespace ServerAPIApp.Core.UseCaseHandlers.Submissions
{
    public class GetByIdCaseHandler : IRequestHandler<GetSubmissionByIdCase, SubmissionDto>
    {
        private ISubmissionRepository _repo;

        public GetByIdCaseHandler(ISubmissionRepository repo)
        {
            _repo = repo;
        }

        public async Task<SubmissionDto> Handle(GetSubmissionByIdCase command, CancellationToken cancellationToken)
        {
            var entity = await _repo.GetByIdAsync(command.Id, cancellationToken);

            if (entity is null)
                throw new ResourceNotFoundException("Resource not found");

            return SubmissionDto.From(entity);
        }
    }
}
