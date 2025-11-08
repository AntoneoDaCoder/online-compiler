using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Contracts.Abstractions
{
    public interface IProblemVersionRepository
    {
        Task<ProblemVersionEntity?> GetByIdAsync
            (Guid versionId,
            CancellationToken cancellationToken = default);
        Task<ProblemVersionEntity?> GetByVersionAndProblemIdsAsync
            (Guid problemId,
            Guid versionId,
            CancellationToken cancellationToken = default);
        Task<List<ProblemVersionEntity>?> GetProblemVersionsByIdAsync
            (Guid problemId,
            CancellationToken cancellationToken = default);
        Task<ProblemVersionEntity> CreateAsync
            (ProblemVersionEntity problemVersion,
            CancellationToken cancellationToken = default);
        Task<ProblemVersionEntity> UpdateAsync
            (ProblemVersionEntity problemVersion,
            CancellationToken cancellationToken = default);
        Task<ProblemVersionEntity> DeleteAsync
            (ProblemVersionEntity problemVersion,
            CancellationToken cancellationToken = default);
    }
}
