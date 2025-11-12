using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Contracts.Abstractions
{
    public interface IProblemVersionLanguageRepository
    {
        Task<ProblemVersionLanguage?> GetByIdsAsync
            (Guid versionId,
            Guid languageId,
            CancellationToken cancellationToken = default);
        Task<List<ProblemVersionLanguage>?> GetAllSupportedAsync
            (Guid versionId,
            CancellationToken cancellationToken = default);
        Task<ProblemVersionLanguage> CreateAsync
            (ProblemVersionLanguage entity,
            CancellationToken cancellationToken = default);
        Task<ProblemVersionLanguage> UpdateAsync
            (ProblemVersionLanguage entity,
            CancellationToken cancellationToken = default);
        Task<bool> DeleteAsync
           (Guid versionId,
            Guid languageId,
            CancellationToken cancellationToken = default);
    }
}
