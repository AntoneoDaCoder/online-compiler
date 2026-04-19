using ServerAPIApp.Domain.Entities;
using System.Linq.Expressions;

namespace ServerAPIApp.Contracts.Abstractions
{
    public interface IProblemRepository : IBaseRepository<ProblemEntity>
    {
        IQueryable<ProblemEntity> Query();
    }
}
