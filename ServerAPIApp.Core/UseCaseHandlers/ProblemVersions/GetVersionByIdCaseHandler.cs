using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Contracts.DTOs;
using ServerAPIApp.Core.UseCases.ProblemVersions;
using ServerAPIApp.Domain.Exceptions.NotFoundExceptions;

namespace ServerAPIApp.Core.UseCaseHandlers.ProblemVersions
{
    public class GetVersionByIdCaseHandler : IRequestHandler<GetVersionByIdCase, UserProblemVersionDto>
    {
        private IProblemVersionRepository _repo;

        public GetVersionByIdCaseHandler(IProblemVersionRepository repo)
        {
            _repo = repo;
        }

        public async Task<UserProblemVersionDto> Handle(GetVersionByIdCase command, CancellationToken cancellationToken)
        {
            var entity = await _repo.GetByIdAsync(command.VersionId, cancellationToken: cancellationToken);

            if (entity is null)
                throw new ResourceNotFoundException("Resource not found");

            return UserProblemVersionDto.From(entity);
        }
    }
}
