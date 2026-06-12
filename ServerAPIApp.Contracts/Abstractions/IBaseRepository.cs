using System.Linq.Expressions;

namespace ServerAPIApp.Contracts.Abstractions
{
    public interface IBaseRepository<T> where T : class
    {
        Task<IList<T>?> GetFilteredAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes);
        Task<IList<T>?> GetPagedFilteredAsync(Expression<Func<T, bool>> predicate, int page, int pageSize,
            CancellationToken cancellationToken = default, params Expression<Func<T, object>>[] includes);
        Task<T> CreateAsync(T entity, CancellationToken cancellationToken = default);
        Task<T?> UpdateAsync(T entity, CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync(T entity, CancellationToken cancellationToken = default);
    }
}
