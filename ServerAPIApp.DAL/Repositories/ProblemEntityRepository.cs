using Microsoft.EntityFrameworkCore;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.DAL.Contexts;
using ServerAPIApp.Domain.Entities;
using System.Threading;

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

        public async Task<bool> SoftDeleteAsync
            (Guid id,
            Guid initiatorId,
            TimeSpan gracePeriod,
            CancellationToken cancellationToken = default)
        {
            var now = DateTimeOffset.UtcNow;

            var affected = await _context.Problems
                .Where(p => p.Id == id && p.IsDeleted == false)
                .ExecuteUpdateAsync(s => s
                    .SetProperty(p => p.IsDeleted, true)
                    .SetProperty(p => p.InitiatorId, initiatorId)
                    .SetProperty(p => p.DeletionScheduledAt, now)
                    .SetProperty(p => p.DeletionDeadline, now + gracePeriod)
                    .SetProperty(p => p.ModifiedAt, now)
                    .SetProperty(p => p.ModifiedBy, initiatorId)
                , cancellationToken);

            return affected > 0;
        }

        public async Task<bool> CancelSoftDeleteAsync
            (Guid id,
            Guid initiatorId,
            CancellationToken cancellationToken = default)
        {
            var affected = await _context.Problems
               .Where(p => p.Id == id && p.IsDeleted == true)
               .ExecuteUpdateAsync(s => s
                   .SetProperty(p => p.IsDeleted, false)
                   .SetProperty(p => p.InitiatorId, (Guid?)null)
                   .SetProperty(p => p.DeletionScheduledAt, (DateTimeOffset?)null)
                   .SetProperty(p => p.DeletionDeadline, (DateTimeOffset?)null)
                   .SetProperty(p => p.ModifiedAt, DateTimeOffset.UtcNow)
                   .SetProperty(p => p.ModifiedBy, initiatorId)
               , cancellationToken);

            return affected > 0;
        }
    }
}
