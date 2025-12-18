using ServerAPIApp.Domain.Entities;
using System.Linq.Expressions;

namespace ServerAPIApp.Contracts.Abstractions
{
    public interface IProblemVersionRepository
    {
        Task<ProblemVersionEntity?> GetByIdAsync
            (Guid versionId,
            bool isDraft = false,
            CancellationToken cancellationToken = default);
        Task<ProblemVersionEntity?> GetByIdWithLanguagesAsync
            (Guid versionId,
            CancellationToken cancellationToken = default);
        Task<List<ProblemVersionEntity>?> GetFilteredWithLanguagesAsync
            (Expression<Func<ProblemVersionEntity, bool>> filter,
            CancellationToken cancellationToken = default);
        Task<ProblemVersionEntity> CreateDraftAsync
            (ProblemVersionEntity draft,
            CancellationToken cancellationToken = default);
        Task<bool> UpdateDraftAsync
            (ProblemVersionEntity draft,
            CancellationToken cancellationToken = default);
        Task<ProblemVersionEntity?> PublishDraftAsync
            (Guid draftId,
            Guid publisherId,
            CancellationToken cancellationToken = default);
        Task<bool> DeleteDraftAsync
            (Guid draftId,
            CancellationToken cancellationToken = default);
    }
}
