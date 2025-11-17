using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.DAL.Confs
{
    public sealed class ProblemVersionEntityConfiguration : IEntityTypeConfiguration<ProblemVersionEntity>
    {
        public void Configure(EntityTypeBuilder<ProblemVersionEntity> builder)
        {
            builder.ToTable("problem_versions");

            builder.HasKey(pv => pv.Id);
            builder.Property(x => x.Id).HasColumnName("id");

            builder.HasOne(pv => pv.Problem)
                .WithMany(p => p.Versions)
                .HasForeignKey(pv => pv.ProblemId)
                .OnDelete(DeleteBehavior.Cascade);


            builder.Property(x => x.CreatedAt).HasColumnName("created_at").IsRequired();
            builder.Property(x => x.CreatedBy).HasColumnName("created_by").IsRequired();

            builder.Property(x => x.IsDraft).HasColumnName("is_draft").IsRequired();
            builder.Property(x => x.IsPublished).HasColumnName("is_published").IsRequired();
            builder.Property(x => x.PublishedBy).HasColumnName("published_by");

            builder.Property(x => x.Version).HasColumnName("version").IsRequired();
            builder.Property(x => x.Statement)
                .HasColumnName("statement")
                .IsRequired()
                .HasColumnType("text");

            builder.Property(x => x.TotalTests).HasColumnName("total_tests").IsRequired();

            builder.Property(x => x.TestTemplateKey)
                .HasColumnName("test_template_key")
                .HasMaxLength(1024)
                .IsUnicode(false)
                .IsRequired();

            builder.HasIndex(x => new { x.ProblemId, x.Version })
            .IsUnique()
            .HasDatabaseName("ux_problem_version_problemid_version");

            builder.HasIndex(x => x.CreatedBy).HasDatabaseName("ix_problem_versions_createdby");
            builder.HasIndex(x => x.CreatedAt).HasDatabaseName("ix_problem_versions_createdat");

            builder.HasMany(x => x.SupportedLanguages)
             .WithOne(x => x.Version)
             .HasForeignKey(x => x.VersionId)
             .OnDelete(DeleteBehavior.Cascade);

            builder.Property(x => x.NumSubmissions)
              .HasColumnName("num_submissions")
              .HasDefaultValue(0)
              .IsRequired();
        }
    }
}
