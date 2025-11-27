using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Contracts.Abstractions
{
    public interface IProblemRepository
    {
        Task<ProblemEntity?> GetByIdAsync
         (Guid problemId,
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
