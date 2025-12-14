using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.DAL.Confs
{
    public sealed class ProblemVersionLanguageConfiguration : IEntityTypeConfiguration<ProblemVersionLanguage>
    {
        public void Configure(EntityTypeBuilder<ProblemVersionLanguage> builder)
        {
            builder.ToTable("problem_languages");

            builder.HasKey(pl => new { pl.VersionId, pl.LanguageId });

            builder.HasOne(pl => pl.Version)
                .WithMany(v => v.SupportedLanguages)
                .HasForeignKey(pl => pl.VersionId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(pl => pl.Language)
                .WithMany(l => l.ProblemVersionLinks)
                .HasForeignKey(pl => pl.LanguageId)
                .OnDelete(DeleteBehavior.Restrict);

            builder.Property(x => x.VersionId).HasColumnName("version_id").IsRequired();
            builder.Property(x => x.LanguageId).HasColumnName("language_id").IsRequired();

            builder.Property(x => x.ArtifactsKey)
                .HasColumnName("artifacts_key")
                .HasMaxLength(1024)
                .IsUnicode(false)
                .IsRequired(false);

            builder.Property(x => x.EntryPoint)
                .HasColumnName("entry_point")
                .HasMaxLength(256)
                .IsUnicode(false)
                .IsRequired(false);

            builder.HasIndex(x => x.LanguageId).HasDatabaseName("ix_pvlang_languageid");
            builder.HasIndex(x => x.VersionId).HasDatabaseName("ix_pvlang_versionid");
        }
    }
}
