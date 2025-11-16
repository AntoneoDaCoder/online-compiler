using ServerAPIApp.Domain.Entities;
using System.Linq.Expressions;

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
        Task<List<ProblemVersionEntity>?> GetFilteredVersionsAsync
            (Expression<Func<ProblemVersionEntity, bool>> filter,
            CancellationToken cancellationToken = default);
        Task<ProblemVersionEntity> CreateDraftAsync
            (ProblemVersionEntity draft,
            CancellationToken cancellationToken = default);
        Task<ProblemVersionEntity?> PublishDraftAsync
            (ProblemVersionEntity draft,
            CancellationToken cancellationToken = default);
        Task<bool> DeleteDraftAsync
            (Guid draftId,
            CancellationToken cancellationToken = default);
    }
}
