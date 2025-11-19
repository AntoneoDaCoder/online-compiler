using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Core.UseCases.Languages;
using System;
using System.Collections.Generic;
using System.Text;

namespace ServerAPIApp.Core.UseCaseHandlers.Languages
{
    public class DeleteCaseHandler : IRequestHandler<DeleteLanguageCase>
    {
        private ILanguageRepository _repo;
        public DeleteCaseHandler(ILanguageRepository repo)
        {
            _repo = repo;
        }

        public async Task Handle(DeleteLanguageCase command, CancellationToken cancellationToken)
        {
            await _repo.DeleteAsync(command.Id, cancellationToken);
        }
    }
}
