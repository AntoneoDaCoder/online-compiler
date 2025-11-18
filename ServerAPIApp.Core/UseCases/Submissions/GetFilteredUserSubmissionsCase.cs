using MediatR;

namespace ServerAPIApp.Core.UseCases.Submissions
{
    public record GetFilteredUserSubmissionsCase(Guid UserId,) : IRequest;
}
