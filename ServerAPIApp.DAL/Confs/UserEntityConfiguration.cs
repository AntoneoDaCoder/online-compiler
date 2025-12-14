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

            builder.Property(u => u.UserName)
                .HasMaxLength(256)
                .HasColumnName("user_name");

            builder.Property(u => u.NormalizedUserName)
                .HasMaxLength(256)
                .HasColumnName("normalized_user_name");

            builder.Property(u => u.Email)
             .HasMaxLength(256)
             .HasColumnName("email")
             .IsRequired(false);

            builder.Property(u => u.NormalizedEmail)
             .HasMaxLength(256)
             .HasColumnName("normalized_email")
             .IsRequired(false);

            builder.Property(u => u.EncryptedEmail)
               .HasColumnName("encrypted_email")
               .HasColumnType("text")
               .IsRequired(false);

            builder.Property(u => u.EmailHash)
                .HasColumnName("email_hash")
                .HasMaxLength(64)
                .IsRequired(false);

            builder.Property(u => u.EmailConfirmed)
             .HasColumnName("email_confirmed");

            builder.Property(u => u.PasswordHash)
             .HasColumnName("password_hash");

            builder.Property(u => u.SecurityStamp)
             .HasMaxLength(256)
             .HasColumnName("security_stamp");

            builder.Property(u => u.ConcurrencyStamp)
             .HasMaxLength(256)
             .HasColumnName("concurrency_stamp")
             .IsConcurrencyToken();

            builder.Property(u => u.PhoneNumber)
             .HasMaxLength(50)
             .HasColumnName("phone_number");

            builder.Property(u => u.PhoneNumberConfirmed)
             .HasColumnName("phone_number_confirmed");

            builder.Property(u => u.TwoFactorEnabled)
             .HasColumnName("two_factor_enabled");

            builder.Property(u => u.LockoutEnd)
             .HasColumnName("lockout_end");

            builder.Property(u => u.LockoutEnabled)
             .HasColumnName("lockout_enabled");

            builder.Property(u => u.AccessFailedCount)
             .HasColumnName("access_failed_count");

            builder.Property(u => u.Name)
             .HasMaxLength(200)
             .HasColumnName("name")
             .IsRequired(false);

            builder.Property(u => u.RefreshToken)
             .HasMaxLength(2048)
             .HasColumnName("refresh_token")
             .IsRequired(false);

            builder.Property(u => u.RefreshTokenExpiryTime)
             .HasColumnName("refresh_token_expiry_time");

            builder.Property(u => u.CreatedAt)
             .HasColumnName("created_at")
             .IsRequired(false);

            builder.Property(u => u.CreatedBy)
             .HasColumnName("created_by")
             .IsRequired(false);

            builder.HasOne(u => u.Creator)
             .WithMany()
             .HasForeignKey(u => u.CreatedBy)
             .OnDelete(DeleteBehavior.Restrict);

            builder.Property(u => u.ModifiedAt)
             .HasColumnName("modified_at")
             .IsRequired(false);

            builder.Property(u => u.ModifiedBy)
             .HasColumnName("modified_by")
             .IsRequired(false);

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

            builder.HasIndex(u => u.NormalizedUserName)
             .IsUnique()
             .HasDatabaseName("ux_users_normalizedusername");

            builder.HasIndex(u => u.EmailHash)
                .IsUnique()
                .HasDatabaseName("ux_users_emailhash");

            builder.HasIndex(u => u.NormalizedEmail)
             .HasDatabaseName("ix_users_normalizedemail");

            builder.HasIndex(u => u.CreatedBy)
             .HasDatabaseName("ix_users_createdby");
        }
    }
}
