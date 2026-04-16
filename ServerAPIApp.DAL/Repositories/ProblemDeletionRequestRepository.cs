using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.DAL.Contexts;
using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.DAL.Repositories
{
    public class ProblemDeletionRequestRepository : BaseRepository<ProblemDeletionRequestEntity>, IProblemDeletionRequestRepository
    {
        public ProblemDeletionRequestRepository(BaseDbContext context) : base(context) { }
    }
}
