using MediatR;
using ServerAPIApp.Domain.Entities;
using ServerAPIApp.Core.UseCases.Submissions;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.Helpers;

namespace ServerAPIApp.Core.UseCaseHandlers.Submissions
{
    public class CreateCaseHandler : IRequestHandler<CreateSubmissionCase, SubmissionEntity>
    {
        private ISubmissionRepository _repo;

        public CreateCaseHandler(ISubmissionRepository repo)
        {
            _repo = repo;
        }

        public async Task<SubmissionEntity> Handle(CreateSubmissionCase command, CancellationToken cancellationToken)
        {
            var entity = command.ToEntity();

            return await _repo.CreateAsync(entity, cancellationToken);
        }
    }
}
