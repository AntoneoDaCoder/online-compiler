using MediatR;
using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Core.UseCases.Submissions
{
    public record GetUserSubmissionsCase(Guid UserId) : IRequest<List<SubmissionEntity>?>;
}
