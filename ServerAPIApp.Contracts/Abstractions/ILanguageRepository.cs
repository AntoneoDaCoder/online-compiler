using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Contracts.Abstractions
{
    public interface ILanguageRepository
    {
        Task<LanguageEntity?> GetByIdAsync
            (Guid id,
            CancellationToken cancellationToken = default);
        Task<LanguageEntity?> GetByCodeAsync
            (string code,
            CancellationToken cancellationToken = default);
        Task<LanguageEntity> CreateAsync
            (LanguageEntity lang,
            CancellationToken cancellationToken = default);
        Task<LanguageEntity> UpdateAsync
            (LanguageEntity lang,
            CancellationToken cancellationToken = default);
        Task<LanguageEntity> DeleteAsync
            (Guid id,
            CancellationToken cancellationToken = default);
    }
}
