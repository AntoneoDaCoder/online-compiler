using MediatR;

namespace ServerAPIApp.Core.UseCases.Submissions
{
    public record GetUserSubmissionsCase(Guid UserId, List<string> TargetLanguages) : IRequest;
}
