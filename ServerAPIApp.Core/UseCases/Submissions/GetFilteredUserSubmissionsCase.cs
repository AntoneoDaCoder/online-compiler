using MediatR;
using ServerAPIApp.Contracts.DTOs;

namespace ServerAPIApp.Core.UseCases.Submissions
{
    public record GetFilteredUserSubmissionsCase(Guid UserId, IEnumerable<string>? LanguageCodes,
        bool? IsSuccessful) : IRequest<IEnumerable<ShortSubmissionDto>?>;
}
