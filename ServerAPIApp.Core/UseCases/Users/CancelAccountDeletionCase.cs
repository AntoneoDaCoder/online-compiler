using MediatR;

namespace ServerAPIApp.Core.UseCases.Users
{
    public record CancelAccountDeletionCase(string UserId, Guid SenderId) : IRequest;
}
