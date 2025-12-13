using ServerAPIApp.Domain.Entities;
using System.Linq.Expressions;

namespace ServerAPIApp.Contracts.Abstractions
{
    public interface IProblemRepository
    {
        Task<ProblemEntity?> GetByIdAsync
         (Guid problemId,
         CancellationToken cancellationToken = default);
        Task<List<ProblemEntity>?> GetFilteredWithLatestVersionsAsync
            (Expression<Func<ProblemEntity, bool>> filter,
            CancellationToken cancellationToken = default);
        Task<ProblemEntity?> GetLatestVersionBySlugAsync
            (string slug,
            CancellationToken cancellationToken = default);
        Task<bool> UpdateAsync
        (ProblemEntity problem,
        CancellationToken cancellationToken = default);
        Task<ProblemEntity> CreateAsync
            (ProblemEntity problem,
            CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync
            (Guid problemId,
            CancellationToken cancellationToken = default);
        Task<bool> SoftDeleteAsync
            (Guid id,
            Guid initiatorId,
            TimeSpan gracePeriod,
            CancellationToken cancellationToken = default);
        Task<bool> CancelSoftDeleteAsync
              (Guid id,
              Guid initiatorId,
              CancellationToken cancellationToken = default);
    }
}
