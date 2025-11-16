using Microsoft.AspNetCore.Identity;
using ServerAPIApp.Domain.Entities;
using System.Linq.Expressions;

namespace ServerAPIApp.Contracts.Abstractions
{
    public interface IUserRepository
    {
        Task<UserEntity?> GetByIdAsync
            (Guid userId,
            CancellationToken cancellationToken = default);
        Task<List<UserEntity>?> GetFilteredUsersAsync
            (Expression<Func<UserEntity, bool>> filter,
            CancellationToken cancellationToken = default);
        Task<UserEntity?> GetByEmailAsync
            (string email,
            CancellationToken cancellationToken = default);
        Task<(UserEntity? user, List<string>? roles)> GetByIdWithRolesAsync
            (Guid id,
            CancellationToken cancellationToken = default);
        Task<(UserEntity? user, List<string>? roles)> GetByEmailWithRolesAsync
            (string email,
            CancellationToken cancellationToken = default);
        Task<IdentityResult> UpdateAsync
            (UserEntity user,
            CancellationToken cancellationToken = default);
        Task<IdentityResult> CreateAsync
            (UserEntity user,
            string password,
            CancellationToken cancellationToken = default);
        Task<IdentityResult> DeleteAsync
            (UserEntity user,
            CancellationToken cancellationToken = default);
        Task<bool> DeleteRangeAsync
            (IEnumerable<UserEntity> users,
            CancellationToken cancellationToken = default);
        Task<bool> CheckPasswordAsync
            (UserEntity user,
            string password,
            CancellationToken cancellationToken = default);
    }
}
