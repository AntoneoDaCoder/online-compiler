using Microsoft.EntityFrameworkCore;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.DAL.Contexts;
using System.Linq.Expressions;
using ServerAPIApp.DAL.Extensions;

namespace ServerAPIApp.DAL.Repositories
{
    public class BaseRepository<T> : IBaseRepository<T> where T : class
    {
        protected BaseDbContext _context;
        protected DbSet<T> _dbSet;

        public BaseRepository(BaseDbContext context)
        {
            _context = context;

            _dbSet = _context.Set<T>();
        }

        public async Task<IList<T>?> GetFilteredAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default,
            params Expression<Func<T, object>>[] includes)
        {
            return await _dbSet.AsNoTracking()
                .Where(predicate)
                .Includes(includes)
                .ToListAsync(cancellationToken);
        }

        public async Task<IList<T>?> GetPagedFilteredAsync(Expression<Func<T, bool>> predicate, int page, int pageSize, CancellationToken cancellationToken = default,
           params Expression<Func<T, object>>[] includes)
        {
            return await _dbSet.AsNoTracking()
                .Where(predicate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Includes(includes)
                .ToListAsync(cancellationToken);
        }

        public async Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default)
        {
            var result = _dbSet.Add(entity);

            await _context.SaveChangesAsync(cancellationToken);

            return result.Entity;
        }

        public async Task<T?> UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            var result = _dbSet.Update(entity);

            await _context.SaveChangesAsync(cancellationToken);

            return result.Entity;
        }

        public async Task<bool> DeleteAsync(T entity, CancellationToken cancellationToken = default)
        {
            var result = _dbSet.Remove(entity);

            await _context.SaveChangesAsync(cancellationToken);

            return result.Entity != null;
        }
    }
}
