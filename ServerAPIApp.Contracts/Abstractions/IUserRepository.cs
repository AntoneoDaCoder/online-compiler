using ServerAPIApp.Domain.Entities;
using System.Linq.Expressions;

namespace ServerAPIApp.Contracts.Abstractions
{
    public interface IUserRepository
    {
        Task<UserEntity?> GetByIdAsync
            (Guid userId,
            CancellationToken cancellationToken = default);

        Task<IEnumerable<UserEntity>?> GetFilteredUsersAsync
            (Expression<Func<UserEntity, bool>> filter,
            CancellationToken cancellationToken = default);

        Task<UserEntity?> UpdateAsync
            (UserEntity user,
            CancellationToken cancellationToken = default);

        Task<UserEntity?> CreateAsync
            (UserEntity user,
            CancellationToken cancellationToken = default);

        Task<UserEntity?> DeleteAsync
            (UserEntity user,
            CancellationToken cancellationToken = default);
    }
}
