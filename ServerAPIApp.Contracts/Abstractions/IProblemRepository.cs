using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Contracts.Abstractions
{
    public interface IProblemRepository
    {
        Task<ProblemEntity?> GetByIdAsync
         (Guid problemId,
         CancellationToken cancellationToken = default);
        Task<ProblemEntity> UpdateAsync
            (ProblemEntity problem,
            CancellationToken cancellationToken = default);
        Task<ProblemEntity> CreateAsync
            (ProblemEntity problem,
            CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync
            (Guid problemId,
            CancellationToken cancellationToken = default);
    }
}
