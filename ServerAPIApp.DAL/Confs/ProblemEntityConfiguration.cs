using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.DAL.Confs
{
    public sealed class ProblemEntityConfiguration : IEntityTypeConfiguration<ProblemEntity>
    {
        public void Configure(EntityTypeBuilder<ProblemEntity> b)
        {
            b.ToTable("problems");

            b.HasKey(x => x.Id);
            b.Property(x => x.Id).HasColumnName("id");

            b.Property(x => x.Slug)
                .IsRequired()
                .HasMaxLength(128)
                .HasColumnName("slug");

            b.Property(x => x.Title)
                .IsRequired()
                .HasMaxLength(512)
                .HasColumnName("title");

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

            b.Property(x => x.IsDeleted)
                .IsRequired()
                .HasColumnName("is_deleted");

            b.Property(x => x.DeletionScheduledAt)
                .HasColumnName("deletion_scheduled_at");

            b.Property(x => x.DeletionDeadline)
                .HasColumnName("deletion_deadline");

            b.Property(x => x.InitiatorId)
                .HasColumnName("initiator_id");

            b.HasOne(x => x.Initiator)
                .WithMany()
                .HasForeignKey(x => x.InitiatorId)
                .OnDelete(DeleteBehavior.Restrict);

            b.Property(x => x.ModifiedAt)
                .HasColumnName("modified_at");

            b.Property(x => x.ModifiedBy)
                .HasColumnName("modified_by");

            b.HasOne(x => x.Editor)
                .WithMany()
                .HasForeignKey(x => x.ModifiedBy)
                .OnDelete(DeleteBehavior.Restrict);

            b.Property(x => x.LastPublishedVersionId)
                .HasColumnName("last_published_version");

            b.HasOne(x => x.LastPublishedVersion)
                .WithMany()
                .HasForeignKey(x => x.LastPublishedVersionId)
                .OnDelete(DeleteBehavior.SetNull);

            b.HasMany(x => x.Versions)
                .WithOne(v => v.Problem)
                .HasForeignKey(v => v.ProblemId)
                .OnDelete(DeleteBehavior.Cascade);

            b.HasIndex(x => x.Slug).IsUnique().HasDatabaseName("ux_problems_slug");
            b.HasIndex(x => x.CreatedBy).HasDatabaseName("ix_problems_createdby");
            b.HasIndex(x => x.IsDeleted).HasDatabaseName("ix_problems_isdeleted");
        }
    }
}
