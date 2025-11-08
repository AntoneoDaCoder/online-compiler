using Microsoft.AspNetCore.Identity;
using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Contracts.Abstractions
{
    public interface IUserRepository
    {
        Task<UserEntity?> GetByIdAsync
            (Guid userId,
            CancellationToken cancellationToken = default);
        Task<UserEntity?> GetByEmailAsync
            (string email,
            CancellationToken cancellationToken = default);
        Task<UserEntity?> GetByIdWithRolesAsync
            (Guid id,
            CancellationToken cancellationToken = default);
        Task<UserEntity?> GetByEmailWithRolesAsync
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
