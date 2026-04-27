using MediatR;
using ServerAPIApp.Contracts.DTOs;

namespace ServerAPIApp.Core.UseCases.Users
{
    public record SoftDeleteAccountCase(string UserId, Guid SenderId) : IRequest<UserMetadataDto?>;
}
