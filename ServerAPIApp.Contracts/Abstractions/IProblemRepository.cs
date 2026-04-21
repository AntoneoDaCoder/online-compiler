using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Contracts.Abstractions
{
    public interface IProblemRepository : IBaseRepository<ProblemEntity>
    {
        IQueryable<ProblemEntity> Query();
        Task SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
