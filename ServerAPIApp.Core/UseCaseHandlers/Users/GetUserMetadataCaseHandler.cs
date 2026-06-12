using MediatR;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.Contracts.DTOs;
using ServerAPIApp.Core.UseCases.Users;
using ServerAPIApp.Domain.Exceptions.NotFoundExceptions;
using ServerAPIApp.Domain.Exceptions.ForbiddenExceptions;

namespace ServerAPIApp.Core.UseCaseHandlers.Users
{
    public class GetUserMetadataCaseHandler : IRequestHandler<GetUserMetadataCase, UserMetadataDto>
    {
        private readonly IUserRepository _repo;

        public GetUserMetadataCaseHandler(IUserRepository repo)
        {
            _repo = repo;
        }

        public async Task<UserMetadataDto> Handle(GetUserMetadataCase request, CancellationToken cancellationToken)
        {
            var parsedUserId = Guid.Parse(request.UserId);

            if (parsedUserId != request.SenderId)
                throw new ForbiddenException("You're not allowed to see other users data");

            var entity = (await _repo.GetFilteredAsync(x => x.Id == parsedUserId, cancellationToken)).FirstOrDefault();

            if (entity == null)
                throw new ResourceNotFoundException("Account not found");

            return UserMetadataDto.From(entity);
        }
    }
}
