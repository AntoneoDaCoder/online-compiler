using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.DAL.Confs
{
    public sealed class UserEntityConfiguration : IEntityTypeConfiguration<UserEntity>
    {
        public void Configure(EntityTypeBuilder<UserEntity> builder)
        {
            builder.ToTable("users");

            builder.HasKey(u => u.Id);
            builder
                .Property(u => u.Id)
                .HasColumnName("id");

            builder.Property(u => u.ExternalProviderId)
                .IsRequired()
                .HasColumnName("external_provider_id");

            builder.Property(u => u.ModifiedAt)
             .HasColumnName("modified_at")
             .IsRequired(false);

            builder.Property(u => u.ModifiedBy)
             .HasColumnName("modified_by")
             .IsRequired(false);

            builder.Property(x => x.DeletionJobId)
                .IsRequired(false)
                .HasColumnName("deletion_job_id");

            builder.HasOne(u => u.Editor)
             .WithMany()
             .HasForeignKey(u => u.ModifiedBy)
             .OnDelete(DeleteBehavior.Restrict);

            builder.Property(u => u.IsDeleted)
             .HasColumnName("is_deleted")
             .IsRequired();

            builder.Property(u => u.DeletionScheduledAt)
             .HasColumnName("deletion_scheduled_at")
             .IsRequired(false);

            builder.Property(u => u.DeletionDeadline)
             .HasColumnName("deletion_deadline")
             .IsRequired(false);

            builder.Property(u => u.InitiatorId)
             .HasColumnName("initiator_id")
             .IsRequired(false);

            builder.HasOne(u => u.Initiator)
             .WithMany()
             .HasForeignKey(u => u.InitiatorId)
             .OnDelete(DeleteBehavior.Restrict);

            builder.HasIndex(u => u.ExternalProviderId)
                .IsUnique()
                .HasDatabaseName("ux_users_external_provider_id");

            builder.HasIndex(x => x.DeletionJobId)
                .HasDatabaseName("ix_users_deletion_job_id");
        }
    }
}
