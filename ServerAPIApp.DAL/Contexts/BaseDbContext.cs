using Microsoft.EntityFrameworkCore;
using ServerAPIApp.Domain.Entities;
using System.Reflection;

namespace ServerAPIApp.DAL.Contexts
{
    public sealed class BaseDbContext : DbContext
    {
        public BaseDbContext(DbContextOptions<BaseDbContext> options) : base(options) { }

        public DbSet<LanguageEntity> Languages { get; set; }
        public DbSet<ProblemEntity> Problems { get; set; }
        public DbSet<ProblemVersionEntity> ProblemVersions { get; set; }
        public DbSet<ProblemVersionLanguage> VersionLanguages { get; set; }
        public DbSet<SubmissionEntity> Submissions { get; set; }
        public DbSet<UserEntity> Users { get; set; }
        public DbSet<ProblemDeletionRequestEntity> DeletionRequests { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());

            base.OnModelCreating(builder);
        }
    }
}
