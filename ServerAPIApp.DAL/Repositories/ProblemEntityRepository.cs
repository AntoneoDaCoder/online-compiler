using Microsoft.EntityFrameworkCore;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.DAL.Contexts;
using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.DAL.Repositories
{
    public class ProblemEntityRepository : BaseRepository<ProblemEntity>, IProblemRepository
    {
        public ProblemEntityRepository(BaseDbContext context) : base(context) { }

        public IQueryable<ProblemEntity> Query()
        {
            return _dbSet.AsQueryable();
        }
    }
}
