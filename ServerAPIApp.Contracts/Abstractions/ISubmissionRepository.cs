using ServerAPIApp.Domain.Entities;
using System.Linq.Expressions;

namespace ServerAPIApp.Contracts.Abstractions
{
    public interface ISubmissionRepository
    {
        Task<SubmissionEntity?> GetByIdAsync
            (Guid id,
            CancellationToken cancellationToken = default);
        Task<List<SubmissionEntity>?> GetUserSubmissionsAsync
            (Guid userId,
            CancellationToken cancellationToken = default);
        Task<List<SubmissionEntity>?> GetUserSubmissionsFilteredByLanguageAsync
            (Guid userId,
            Expression<Func<string, bool>> languageFilter,
            CancellationToken cancellationToken = default);
        Task<SubmissionEntity> CreateAsync
            (SubmissionEntity entity,
            CancellationToken cancellationToken = default);
        Task<SubmissionEntity> UpdateAsync
           (SubmissionEntity entity,
           CancellationToken cancellationToken = default);
        Task<SubmissionEntity> DeleteAsync
           (SubmissionEntity entity,
            CancellationToken cancellationToken = default);
    }
}
