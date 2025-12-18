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

        public async Task<ProblemVersionEntity?> GetByIdWithLanguagesAsync
            (Guid versionId,
            CancellationToken cancellationToken = default)
        {
            return await _context.ProblemVersions
                .AsNoTracking()
                .Where(x => x.Id == versionId)
                .Include(x => x.SupportedLanguages)
                .ThenInclude(x => x.Language)
                .FirstOrDefaultAsync(cancellationToken);
        }

        public async Task<ProblemVersionEntity> CreateDraftAsync
            (ProblemVersionEntity draft,
            CancellationToken cancellationToken = default)
        {
            var entry = _context.ProblemVersions.Add(draft);

            await _context.SaveChangesAsync(cancellationToken);

            return entry.Entity;
        }

        public async Task<bool> UpdateDraftAsync
            (ProblemVersionEntity draft,
            CancellationToken cancellationToken = default)
        {
            var affected = await _context.ProblemVersions
                .Where(x => x.Id == draft.Id && x.ProblemId == draft.ProblemId && x.IsDraft)
                .ExecuteUpdateAsync
                (
                    x => x
                    .SetProperty(x => x.Statement, draft.Statement)
                    .SetProperty(x => x.TotalTests, draft.TotalTests)
                    .SetProperty(x => x.TestTemplateKey, draft.TestTemplateKey),
                    cancellationToken
                );

            return affected > 0;
        }

        public async Task<ProblemVersionEntity?> PublishDraftAsync(Guid draftId, Guid publisherId, CancellationToken cancellationToken = default)
        {
            await using var tx = await _context.Database.BeginTransactionAsync(cancellationToken);
            try
            {
                // Сначала заблокировать строку draft в problem_versions
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT 1 FROM problem_versions WHERE id = {draftId} FOR UPDATE",
                    cancellationToken);

                // Получаем draft (теперь он заблокирован и трекется)
                var draft = await _context.ProblemVersions
                    .FirstOrDefaultAsync(v => v.Id == draftId, cancellationToken);

                if (draft == null || !draft.IsDraft)
                {
                    await tx.RollbackAsync(cancellationToken);
                    Console.WriteLine("[API] Couldn't find the draft or it's already published");
                    return null;
                }

                // Теперь блокируем связанную проблему по корректному problemId
                await _context.Database.ExecuteSqlInterpolatedAsync(
                    $"SELECT 1 FROM problems WHERE id = {draft.ProblemId} FOR UPDATE",
                    cancellationToken);

                // Вычисляем последнюю опубликованную версию
                var latestVersion = await _context.ProblemVersions
                    .Where(v => v.ProblemId == draft.ProblemId && v.IsPublished)
                    .MaxAsync(v => (int?)v.Version, cancellationToken) ?? 0;

                var nextLatest = latestVersion + 1;
                var now = DateTimeOffset.UtcNow;

                // Меняем трекнутую сущность
                draft.Version = nextLatest;
                draft.IsDraft = false;
                draft.IsPublished = true;
                draft.PublishedBy = publisherId;

                // Обновляем проблему
                var problem = await _context.Problems.FirstOrDefaultAsync(p => p.Id == draft.ProblemId, cancellationToken);
                if (problem == null)
                {
                    await tx.RollbackAsync(cancellationToken);
                    Console.WriteLine("[API] Couldn't find the problem");
                    return null;
                }

                problem.LastPublishedVersionId = draft.Id;
                problem.ModifiedAt = now;
                problem.ModifiedBy = publisherId;

                await _context.SaveChangesAsync(cancellationToken);
                await tx.CommitAsync(cancellationToken);

                return await _context.ProblemVersions
                    .AsNoTracking()
                    .FirstOrDefaultAsync(v => v.Id == draftId, cancellationToken);
            }
            catch (Exception ex)
            {
                Console.WriteLine("[API] Couldn't publish the draft: " + ex);
                try { await tx.RollbackAsync(cancellationToken); } catch { }
                return null;
            }
        }



        public async Task<List<ProblemVersionEntity>?> GetFilteredWithLanguagesAsync
            (Expression<Func<ProblemVersionEntity, bool>> filter,
            CancellationToken cancellationToken = default)
        {
            var entries = await _context.ProblemVersions
                .AsNoTracking()
                .Where(filter)
                .Include(x => x.SupportedLanguages)
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
