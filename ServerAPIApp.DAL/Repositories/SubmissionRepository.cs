using Microsoft.EntityFrameworkCore;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.DAL.Contexts;
using ServerAPIApp.Domain.Entities;
using System.Linq.Expressions;

namespace ServerAPIApp.DAL.Repositories
{
    public class SubmissionRepository : ISubmissionRepository
    {
        private BaseDbContext _context;

        public SubmissionRepository(BaseDbContext context)
        {
            _context = context;
        }

        public async Task<SubmissionEntity?> GetByIdAsync
            (Guid id,
            CancellationToken cancellationToken = default)
        {
            var entity = await _context.Submissions
                .AsNoTracking()
                .FirstOrDefaultAsync(s => s.Id == id, cancellationToken);

            return entity;
        }

        public async Task<List<SubmissionEntity>?> GetUserSubmissionsAsync
            (Guid userId,
            CancellationToken cancellationToken = default)
        {
            var entries = await _context.Submissions
                .AsNoTracking()
                .Where(s => s.CreatedBy == userId)
                .ToListAsync(cancellationToken);

            return entries;
        }

        public async Task<List<SubmissionEntity>?> GetUserSubmissionsFilteredByLanguageAsync
            (Guid userId,
            Expression<Func<SubmissionEntity, bool>> filter,
            CancellationToken cancellationToken = default)
        {
            var entries = await _context.Submissions
                .AsNoTracking()
                .Where(filter)
                .ToListAsync(cancellationToken);

            return entries;
        }

        public async Task<SubmissionEntity> CreateAsync
            (SubmissionEntity entity,
            CancellationToken cancellationToken = default)
        {
            var entry = _context.Submissions.Add(entity);

            await _context.SaveChangesAsync(cancellationToken);

            return entry.Entity;
        }

        public async Task<bool> DeleteAsync
            (Guid id,
            CancellationToken cancellationToken = default)
        {
            var affected = await _context.Submissions
                .Where(x => x.Id == id)
                .ExecuteDeleteAsync(cancellationToken);

            var entry = _context.Submissions.Local.FirstOrDefault(x => x.Id == id);

            if (entry is not null)
                _context.Entry(entry).State = EntityState.Detached;

            return affected > 0;
        }
    }
}
