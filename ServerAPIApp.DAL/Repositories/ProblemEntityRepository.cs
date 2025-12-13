using Microsoft.EntityFrameworkCore;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.DAL.Contexts;
using ServerAPIApp.Domain.Entities;
using System.Linq.Expressions;
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

        public async Task<List<ProblemEntity>?> GetFilteredWithLatestVersionsAsync
            (Expression<Func<ProblemEntity, bool>> filter,
            CancellationToken cancellationToken = default)
        {
            var entities = await _context.Problems
                .AsNoTracking()
                .Where(filter)
                .Include(x => x.LastPublishedVersion)
                .ThenInclude(x => x.SupportedLanguages)
                .ToListAsync(cancellationToken);

            return entities;
        }

        public async Task<ProblemEntity?> GetLatestVersionBySlugAsync
            (string slug,
            CancellationToken cancellationToken = default)
        {
            var entry = await _context.Problems
                .AsNoTracking()
                .Where(x => x.Slug == slug)
                .Include(x => x.LastPublishedVersion)
                .ThenInclude(x => x.SupportedLanguages)
                .FirstOrDefaultAsync(cancellationToken);

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

        public async Task<bool> UpdateAsync
            (ProblemEntity entity,
            CancellationToken cancellationToken = default)
        {
            var affected = await _context.Problems
                .Where(x => x.Id == entity.Id)
                .ExecuteUpdateAsync
                (
                    x => x
                    .SetProperty(x => x.Slug, entity.Slug)
                    .SetProperty(x => x.Title, entity.Title)
                    .SetProperty(x => x.ModifiedAt, entity.ModifiedAt)
                    .SetProperty(x => x.ModifiedBy, entity.ModifiedBy),
                    cancellationToken
                );

            return affected > 0;
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
                   .SetProperty(p => p.DeletionScheduledAt, DateTimeOffset.MinValue)
                   .SetProperty(p => p.DeletionDeadline, DateTimeOffset.MinValue)
                   .SetProperty(p => p.ModifiedAt, DateTimeOffset.UtcNow)
                   .SetProperty(p => p.ModifiedBy, initiatorId)
               , cancellationToken);

            return affected > 0;
        }
    }
}
