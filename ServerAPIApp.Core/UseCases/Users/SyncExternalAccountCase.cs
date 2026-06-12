using MediatR;

namespace ServerAPIApp.Core.UseCases.Users
{
    public record SyncExternalAccountCase(string ProviderId) : IRequest;
}
