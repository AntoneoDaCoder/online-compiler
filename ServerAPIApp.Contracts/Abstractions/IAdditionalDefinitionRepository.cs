using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Contracts.Abstractions
{
    public interface IAdditionalDefinitionRepository
    {
        Task<AdditionalDefinitionEntity?> GetByIdAsync
            (Guid id,
            CancellationToken cancellationToken = default);
        Task<List<AdditionalDefinitionEntity>?> GetAllForVersionAsync
            (Guid versionId,
            CancellationToken cancellationToken = default);
        Task<List<AdditionalDefinitionEntity>?> GetFilteredByLanguageAsync
            (Guid versionId,
            Func<AdditionalDefinitionEntity, bool> languageFilter,
            CancellationToken cancellationToken = default);
        Task<AdditionalDefinitionEntity> CreateAsync
            (AdditionalDefinitionEntity entity,
            CancellationToken cancellationToken = default);
        Task<AdditionalDefinitionEntity> UpdateAsync
            (AdditionalDefinitionEntity entity,
            CancellationToken cancellationToken = default);
        Task<AdditionalDefinitionEntity> DeleteAsync
            (Guid id,
            CancellationToken cancellationToken = default);
    }
}
