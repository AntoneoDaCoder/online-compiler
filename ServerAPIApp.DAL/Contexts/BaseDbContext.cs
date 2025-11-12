using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using ServerAPIApp.Domain.Entities;
using System.Reflection;

namespace ServerAPIAPP.DAL.Contexts
{
    public sealed class BaseDbContext : IdentityDbContext<UserEntity, IdentityRole<Guid>, Guid>
    {
        public BaseDbContext(DbContextOptions<BaseDbContext> options) : base(options) { }

        public DbSet<LanguageEntity> Languages { get; set; }
        public DbSet<ProblemEntity> Problems { get; set; }
        public DbSet<ProblemVersionEntity> ProblemVersions { get; set; }
        public DbSet<ProblemVersionLanguage> VersionLanguages { get; set; }
        public DbSet<SubmissionEntity> Submissions { get; set; }

        protected override void OnModelCreating(ModelBuilder builder)
        {
            builder.ApplyConfigurationsFromAssembly(Assembly.GetExecutingAssembly());
            base.OnModelCreating(builder);
        }
    }
}
