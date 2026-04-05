using Microsoft.EntityFrameworkCore;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.DAL.Contexts;
using ServerAPIApp.Domain.Entities;
using System.Linq.Expressions;

namespace ServerAPIApp.DAL.Repositories
{
    public class UserRepository : IUserRepository
    {
        private BaseDbContext _context;

        public UserRepository(BaseDbContext context)
        {
            _context = context;
        }

        public async Task<UserEntity?> GetByIdAsync
            (Guid userId,
            CancellationToken cancellationToken = default)
        {
            var user = await _context.Users
                .AsNoTracking()
                .Where(x => x.Id == userId)
                .FirstOrDefaultAsync(cancellationToken);

            return user;
        }

        public async Task<IEnumerable<UserEntity>?> GetFilteredUsersAsync
            (Expression<Func<UserEntity, bool>> filter,
            CancellationToken cancellationToken = default)
        {
            var entries = await _context.Users
                .AsNoTracking()
                .Where(filter)
                .ToListAsync(cancellationToken);

            return entries;
        }

        public async Task<UserEntity?> CreateAsync
            (UserEntity user, CancellationToken cancellationToken = default)
        {
            var result = _context.Users.Add(user);

            await _context.SaveChangesAsync(cancellationToken);

            return result?.Entity;
        }

        public async Task<UserEntity?> UpdateAsync
            (UserEntity user,
            CancellationToken cancellationToken = default)
        {
            var result = _context.Users.Update(user);

            await _context.SaveChangesAsync(cancellationToken);

            return result?.Entity;
        }

        public async Task<UserEntity?> DeleteAsync
            (UserEntity userStub,
            CancellationToken cancellationToken = default)
        {
            var result = _context.Users.Remove(userStub);

            await _context.SaveChangesAsync(cancellationToken);

            return result?.Entity;
        }
    }
}
