using MediatR;

namespace ServerAPIApp.Core.UseCases.Users
{
    public record SoftDeleteAccountCase(string UserId, Guid SenderId) : IRequest;
}
