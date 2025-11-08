using Microsoft.AspNetCore.Identity;
using ServerAPIApp.Contracts.DTOs;
using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Contracts.Abstractions
{
    public interface IUserRepository
    {
        Task<DisplayUserDto?> GetByIdShortenedAsync
            (Guid userId,
            CancellationToken cancellationToken = default);
        Task<UserEntity?> GetByIdFullAsync
            (Guid userId,
            CancellationToken cancellationToken = default);

        Task<DisplayUserDto?> GetByEmailShortenedAsync
            (string email,
            CancellationToken cancellationToken = default);
        Task<UserEntity?> GetByEmailFullAsync
            (string email,
            CancellationToken cancellationToken = default);

        Task<IdentityResult> UpdateAsync
            (UserEntity user,
            CancellationToken cancellationToken = default);
        Task<IdentityResult> CreateAsync
            (UserEntity user,
            CancellationToken cancellationToken = default);
        Task<IdentityResult> DeleteAsync
            (UserEntity user,
            CancellationToken cancellationToken = default);
        Task<bool> CheckPasswordAsync
            (UserEntity user,
            string password,
            CancellationToken cancellationToken = default);
    }
}
