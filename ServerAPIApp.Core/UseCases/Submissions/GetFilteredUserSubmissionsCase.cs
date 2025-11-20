using MediatR;
using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Core.UseCases.Submissions
{
    public record GetFilteredUserSubmissionsCase(Guid UserId, List<string> LanguageCodes) : IRequest<List<SubmissionEntity>?>;
}
