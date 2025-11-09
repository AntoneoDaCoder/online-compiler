using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Contracts.Abstractions
{
    public interface ITestCaseRepository
    {
        Task<TestCaseEntity?> GetByIdAsync
            (Guid caseId,
            CancellationToken cancellationToken = default);
        Task<List<TestCaseEntity>?> GetAllForVersionAsync
            (Guid versionId,
            CancellationToken cancellationToken = default);
        Task<List<TestCaseEntity>?> GetFilteredByLanguageAsync
            (Guid versionId,
            Func<bool, TestCaseEntity> languageFilter,
            CancellationToken cancellationToken = default);
        Task<TestCaseEntity> CreateAsync
            (TestCaseEntity testCase,
            CancellationToken cancellationToken = default);
        Task<TestCaseEntity> UpdateAsync
           (TestCaseEntity testCase,
           CancellationToken cancellationToken = default);
        Task<TestCaseEntity> DeleteAsync
           (Guid caseId,
            CancellationToken cancellationToken = default);
    }
}
