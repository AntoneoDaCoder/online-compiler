using Microsoft.EntityFrameworkCore;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.DAL.Contexts;
using ServerAPIApp.Domain.Entities;
using System.Data;
using System.Linq.Expressions;

namespace ServerAPIApp.DAL.Repositories
{
    public class ProblemVersionEntityRepository : IProblemVersionRepository
    {
        private BaseDbContext _context;

        public ProblemVersionEntityRepository(BaseDbContext context)
        {
            _context = context;
        }

        public async Task<ProblemVersionEntity?> GetByIdAsync
            (Guid id,
            bool isDraft = false,
            CancellationToken cancellationToken = default)
        {
            var query = _context.ProblemVersions
                .AsNoTracking()
                .Where(x => x.Id == id);

            if (isDraft)
            {
                query = query.Where(x => x.IsDraft)
                    .Include(x => x.SupportedLanguages)
                    .ThenInclude(x => x.Language);
            }

            var entity = await query.FirstOrDefaultAsync(cancellationToken);

            return entity;
        }

        public async Task<ProblemVersionEntity> CreateDraftAsync
            (ProblemVersionEntity draft,
            CancellationToken cancellationToken = default)
        {
            var entry = _context.ProblemVersions.Add(draft);

            await _context.SaveChangesAsync(cancellationToken);

            return entry.Entity;
        }

        public async Task<ProblemVersionEntity> UpdateDraftAsync
            (ProblemVersionEntity draft,
            CancellationToken cancellationToken = default)
        {
            var entry = _context.ProblemVersions.Update(draft);

            await _context.SaveChangesAsync(cancellationToken);

            return entry.Entity;
        }

        public async Task<ProblemVersionEntity?> PublishDraftAsync
            (ProblemVersionEntity draft,
            CancellationToken cancellationToken = default)
        {
            await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                await _context.Database.ExecuteSqlInterpolatedAsync
                    ($"SELECT 1 FROM problems WHERE id={draft.ProblemId} FOR UPDATE", cancellationToken);

                //we have to get problem entity here, because if we don't shit can happen
                //ig 2 separate http requests querying on the same problem and both are trying to publish a draft (mind you, fucking Version is an unique index)
                //chaos ensued, server ded, users mad, dev fixit!11!!!111111!!1 (ong)
                var latestVersion = await _context.ProblemVersions
                    .Where(v => v.ProblemId == draft.ProblemId && v.IsPublished)
                    .MaxAsync(v => (int?)v.Version, cancellationToken) ?? 0;

                var nextLatest = latestVersion + 1;
                var now = DateTimeOffset.UtcNow;

                var updated = await _context.Database
                    .ExecuteSqlInterpolatedAsync
                    ($@"
                    UPDATE problem_versions
                    SET version = {nextLatest},
                        is_draft = FALSE,
                        is_published = TRUE,
                        published_by = {draft.PublishedBy},
                        modified_at = {now}
                    WHERE id = {draft.Id} AND is_draft = TRUE"
                    , cancellationToken);

                if (updated == 0)
                {
                    await tx.RollbackAsync(cancellationToken);
                    return null;
                }

                var problem = await _context.Problems.FirstOrDefaultAsync(p => p.Id == draft.ProblemId, cancellationToken);
                if (problem == null)
                {
                    await tx.RollbackAsync(cancellationToken);
                    return null;
                }

                problem.LastPublishedVersionId = draft.Id;
                problem.ModifiedAt = now;

                await _context.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);


                return await _context.ProblemVersions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.Id == draft.Id, cancellationToken);
            }
            catch
            {
                try { await tx.RollbackAsync(cancellationToken); } catch { }
                return null;
            }
        }

        public async Task<List<ProblemVersionEntity>?> GetFilteredAsync
            (Guid problemId,
            Expression<Func<ProblemVersionEntity, bool>>? filter = null,
            CancellationToken cancellationToken = default)
        {
            var query = _context.ProblemVersions.AsNoTracking()
                .Where(x => x.ProblemId == problemId);

            if (filter is not null)
            {
                query = query.Where(filter);
            }

            var entries = await query
                .Include(x => x.Creator)
                .ToListAsync(cancellationToken);

            return entries;
        }

        public async Task<bool> DeleteDraftAsync
            (Guid draftId,
            CancellationToken cancellationToken = default)
        {
            var affected = await _context.ProblemVersions
                .Where(x => x.Id == draftId && x.IsDraft)
                .ExecuteDeleteAsync(cancellationToken);

            var entry = _context.ProblemVersions.Local.FirstOrDefault(x => x.Id == draftId && x.IsDraft);

            if (entry is not null)
                _context.Entry(entry).State = EntityState.Detached;

            return affected > 0;
        }
    }
}
