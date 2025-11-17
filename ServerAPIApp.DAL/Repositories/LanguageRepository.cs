using Microsoft.EntityFrameworkCore;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.DAL.Contexts;
using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.DAL.Repositories
{
    public class LanguageRepository : ILanguageRepository
    {
        private BaseDbContext _context;
        public LanguageRepository(BaseDbContext context)
        {
            _context = context;
        }

        public async Task<LanguageEntity?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var entity = await _context.Languages
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id, cancellationToken);

            return entity;
        }

        public async Task<LanguageEntity?> GetByCodeAsync(string code, CancellationToken cancellationToken = default)
        {
            var entity = await _context.Languages
                .AsNoTracking()
                .FirstOrDefaultAsync(x => x.Code == code, cancellationToken);

            return entity;
        }

        public async Task<LanguageEntity> CreateAsync(LanguageEntity entity, CancellationToken cancellationToken = default)
        {
            var entry = _context.Languages.Add(entity);

            await _context.SaveChangesAsync(cancellationToken);

            return entry.Entity;
        }

        public async Task<LanguageEntity> UpdateAsync(LanguageEntity entity, CancellationToken cancellationToken = default)
        {
            var entry = _context.Languages.Update(entity);

            await _context.SaveChangesAsync(cancellationToken);

            return entry.Entity;
        }

        public async Task<bool> DeleteAsync(Guid id, CancellationToken cancellationToken = default)
        {
            var affected = await _context.Languages
                .Where(x => x.Id == id)
                .ExecuteDeleteAsync(cancellationToken);

            var entry = _context.Languages.Local.FirstOrDefault(x => x.Id == id);

            if (entry is not null)
                _context.Entry(entry).State = EntityState.Detached;

            return affected > 0;
        }
    }
}
