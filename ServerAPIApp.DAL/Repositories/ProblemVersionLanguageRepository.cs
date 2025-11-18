using Microsoft.EntityFrameworkCore;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.DAL.Contexts;
using ServerAPIApp.Domain.Entities;
using System;

namespace ServerAPIApp.DAL.Repositories
{
    public class ProblemVersionLanguageRepository : IProblemVersionLanguageRepository
    {
        private BaseDbContext _context;

        public ProblemVersionLanguageRepository(BaseDbContext context)
        {
            _context = context;
        }

        public async Task<ProblemVersionLanguage?> GetByIdsAsync
             (Guid versionId,
             Guid languageId,
             CancellationToken cancellationToken = default)
        {
            var entity = await _context.VersionLanguages
                .AsNoTracking()
                .FirstOrDefaultAsync(pv => pv.VersionId == versionId && pv.LanguageId == languageId, cancellationToken);

            return entity;
        }

        public async Task<List<ProblemVersionLanguage>?> GetAllSupportedAsync
            (Guid versionId,
            CancellationToken cancellationToken = default)
        {
            var entities = await _context.VersionLanguages
                .AsNoTracking()
                .Where(pv => pv.VersionId == versionId)
                .ToListAsync(cancellationToken);

            return entities;
        }

        public async Task<ProblemVersionLanguage> CreateAsync
            (ProblemVersionLanguage entity,
            CancellationToken cancellationToken = default)
        {
            var entry = _context.VersionLanguages.Add(entity);

            await _context.SaveChangesAsync(cancellationToken);

            return entry.Entity;
        }

        public async Task<ProblemVersionLanguage> UpdateAsync
            (ProblemVersionLanguage entity,
            CancellationToken cancellationToken = default)
        {
            var entry = _context.VersionLanguages.Update(entity);

            await _context.SaveChangesAsync(cancellationToken);

            return entry.Entity;
        }

        public async Task<bool> DeleteAsync
            (Guid versionId,
            Guid languageId,
            CancellationToken cancellationToken = default)
        {
            var affected = await _context.VersionLanguages
                .Where(x => x.VersionId == versionId && x.LanguageId == languageId)
                .ExecuteDeleteAsync(cancellationToken);

            var entry = _context.VersionLanguages.Local.FirstOrDefault(x => x.VersionId == versionId && x.LanguageId == languageId);

            if (entry is not null)
                _context.Entry(entry).State = EntityState.Detached;

            return affected > 0;
        }

        public async Task CreateRangeAsync
            (IEnumerable<ProblemVersionLanguage> range,
            CancellationToken cancellationToken = default)
        {
            await _context.VersionLanguages.AddRangeAsync(range, cancellationToken);

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<ProblemVersionLanguage>> DeleteRangeAsync
            (IEnumerable<ProblemVersionLanguage> range,
            CancellationToken cancellationToken = default)
        {
            await _context.VersionLanguages
                       .Where(x => x.VersionId == versionId && toRemove.Contains(x.LanguageId))
                       .ExecuteDeleteAsync(cancellationToken);

            var trackedRemoved = _context.VersionLanguages.Local
                .Where(x => x.VersionId == versionId && toRemove.Contains(x.LanguageId))
                .ToList();
            foreach (var t in trackedRemoved) _context.Entry(t).State = EntityState.Detached;
        }
    }
}
