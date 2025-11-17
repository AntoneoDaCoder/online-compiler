using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.DAL.Confs
{
    public sealed class LanguageEntityConfiguration : IEntityTypeConfiguration<LanguageEntity>
    {
        public void Configure(EntityTypeBuilder<LanguageEntity> builder)
        {
            builder.ToTable("languages");

            builder.HasKey(l => l.Id);
            builder.Property(l => l.Id).HasColumnName("id");

            builder.Property(l => l.DisplayName)
                .IsRequired()
                .HasMaxLength(200)
                .HasColumnName("display_name");

            builder.Property(l => l.Code)
                .IsRequired()
                .HasMaxLength(30)
                .HasColumnName("code");

            builder.HasIndex(l => l.Code)
                .IsUnique()
                .HasDatabaseName("ux_languages_code");
        }
    }
}
