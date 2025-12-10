using ServerAPIApp.Domain.Entities;
using System.Linq.Expressions;

namespace ServerAPIApp.Contracts.Abstractions
{
    public interface ISubmissionRepository
    {
        Task<SubmissionEntity?> GetByIdAsync
            (Guid id,
            CancellationToken cancellationToken = default);
        Task<List<SubmissionEntity>?> GetFilteredUserSubmissionsAsync
            (Expression<Func<SubmissionEntity, bool>> languageFilter,
            CancellationToken cancellationToken = default);
        Task<SubmissionEntity> CreateAsync
            (SubmissionEntity entity,
            CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync
           (Guid submissionId,
            CancellationToken cancellationToken = default);
    }
}
