using Microsoft.EntityFrameworkCore;
using ServerAPIApp.Contracts.Abstractions;
using ServerAPIApp.DAL.Contexts;
using ServerAPIApp.Domain.Entities;
using System.Data;

namespace ServerAPIApp.DAL.Repositories
{
    public class ProblemVersionEntityRepository : BaseRepository<ProblemVersionEntity>, IProblemVersionRepository
    {
        public ProblemVersionEntityRepository(BaseDbContext context) : base(context) { }

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

        public IQueryable<ProblemVersionEntity> Query()
        {
            return _dbSet.AsQueryable();
        }
    }
}
