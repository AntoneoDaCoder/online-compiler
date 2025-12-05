using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.DAL.Confs
{
    public sealed class SubmissionEntityConfiguration : IEntityTypeConfiguration<SubmissionEntity>
    {
        public void Configure(EntityTypeBuilder<SubmissionEntity> b)
        {
            b.ToTable("submissions");

            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");

            b.Property(x => x.ProblemVersionId)
                .IsRequired()
                .HasColumnName("problem_version_id");

            b.HasOne(x => x.ProblemVersion)
                .WithMany()
                .HasForeignKey(x => x.ProblemVersionId)
                .OnDelete(DeleteBehavior.Restrict);

            b.Property(x => x.CreatedAt)
                .IsRequired()
                .HasColumnName("created_at");

            b.Property(x => x.CreatedBy)
                .IsRequired()
                .HasColumnName("created_by");

            b.HasOne(x => x.Creator)
                .WithMany()
                .HasForeignKey(x => x.CreatedBy)
                .OnDelete(DeleteBehavior.Restrict);

            b.Property(x => x.Solution)
                .HasColumnName("solution")
                .HasColumnType("text")
                .IsRequired();

            b.Property(x => x.BriefStatus)
                .HasColumnName("brief_status")
                .HasMaxLength(30)
                .IsRequired();

            b.Property(x => x.PassedTests)
                .HasColumnName("passed_tests")
                .IsRequired();

            b.Property(x => x.TotalTests)
                .HasColumnName("total_tests")
                .IsRequired();

            b.Property(x => x.SolutionLanguage)
                .HasColumnName("solution_language")
                .HasMaxLength(100)
                .IsRequired();

            b.HasIndex(x => x.CreatedBy).HasDatabaseName("ix_submissions_createdby");
            b.HasIndex(x => x.ProblemVersionId).HasDatabaseName("ix_submissions_problemversion");
            b.HasIndex(x => x.CreatedAt).HasDatabaseName("ix_submissions_createdat");
        }
    }
}
