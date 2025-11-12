using Microsoft.EntityFrameworkCore;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.DAL.Contexts;
using ServerAPIApp.Domain.Entities;

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
            CancellationToken cancellationToken = default)
        {
            var entity = await _context.ProblemVersions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            return entity;
        }

        public async Task<ProblemVersionEntity?> GetByVersionAndProblemIdsAsync
            (Guid problemId,
            Guid versionId,
            CancellationToken cancellationToken = default)
        {
            var entry = await _context.ProblemVersions
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.ProblemId == problemId && x.Id == versionId, cancellationToken);

            return entry;
        }

        public async Task<List<ProblemVersionEntity>?> GetProblemVersionsByIdAsync
            (Guid problemId,
            CancellationToken cancellationToken = default)
        {
            var entries = await _context.ProblemVersions
                .AsNoTracking()
                .Where(x => x.ProblemId == problemId)
                .ToListAsync(cancellationToken);

            return entries;
        }

        public async Task<ProblemVersionEntity> CreateAsync
            (ProblemVersionEntity entity,
            CancellationToken cancellationToken = default)
        {
            var entry = _context.ProblemVersions.Add(entity);

            await _context.SaveChangesAsync(cancellationToken);

            return entry.Entity;
        }
    }
}
