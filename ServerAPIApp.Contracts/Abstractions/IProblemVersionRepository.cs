using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.Contracts.Abstractions
{
    public interface IProblemVersionRepository : IBaseRepository<ProblemVersionEntity>
    {
        Task<ProblemVersionEntity?> PublishDraftAsync
            (Guid draftId,
            Guid publisherId,
            CancellationToken cancellationToken = default);

        IQueryable<ProblemVersionEntity> Query();
    }
}
