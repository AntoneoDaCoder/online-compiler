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
        Task<UserEntity?> GetByHashedEmailAsync
            (string email,
            CancellationToken cancellationToken = default);
        Task<IdentityResult> AddToRolesAsync
            (UserEntity user,
            IEnumerable<string> roles,
            CancellationToken cancellationToken = default);
        Task<IdentityResult> RemoveFromRolesAsync
            (UserEntity user,
           IEnumerable<string> roles,
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
        Task<(UserEntity? user, List<string>? roles)> GetByHashedEmailWithRolesAsync
            (string email,
            CancellationToken cancellationToken = default);
        Task<(UserEntity? user, List<string>? roles)> GetByLoginWithRolesAsync
            (string provider,
            string providerKey,
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
        Task<bool> CheckPasswordAsync
            (UserEntity user,
            string password,
            CancellationToken cancellationToken = default);
        Task<UserEntity?> FindByLoginAsync
            (string provider,
            string providerKey,
            CancellationToken cancellationToken = default);
        Task<bool> AddLoginAsync
            (UserEntity user,
            string provider,
            string providerKey,
            string? displayName = null,
            CancellationToken cancellationToken = default);
        Task<bool> RemoveLoginAsync
            (UserEntity user,
            string provider,
            string providerKey,
            CancellationToken cancellationToken = default);
        Task<IList<UserLoginInfo>> GetUserLoginsAsync
            (UserEntity user,
            CancellationToken cancellationToken = default);

    }
}
