using Microsoft.EntityFrameworkCore;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.DAL.Contexts;
using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.DAL.Repositories
{
    public class ProblemEntityRepository : IProblemRepository
    {
        private BaseDbContext _context;

        public ProblemEntityRepository(BaseDbContext context)
        {
            _context = context;
        }

        public async Task<ProblemEntity?> GetByIdAsync
            (Guid id,
            CancellationToken cancellationToken = default)
        {
            var entry = await _context.Problems
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            return entry;
        }

        public async Task<ProblemEntity> CreateAsync
            (ProblemEntity entity,
            CancellationToken cancellationToken = default)
        {
            var entry = _context.Problems.Add(entity);

            await _context.SaveChangesAsync(cancellationToken);

            return entry.Entity;
        }

        public async Task<ProblemEntity> UpdateAsync
            (ProblemEntity entity,
            CancellationToken cancellationToken = default)
        {
            var entry = _context.Problems.Update(entity);

            await _context.SaveChangesAsync(cancellationToken);

            return entry.Entity;
        }

        public async Task<bool> DeleteAsync
            (Guid id,
            CancellationToken cancellationToken = default)
        {
            var affected = await _context.Problems
                .Where(x => x.Id == id)
                .ExecuteDeleteAsync(cancellationToken);

            var entry = _context.Problems.Local.FirstOrDefault(x => x.Id == id);

            if (entry is not null)
                _context.Entry(entry).State = EntityState.Detached;

            return affected > 0;
        }
    }
}
