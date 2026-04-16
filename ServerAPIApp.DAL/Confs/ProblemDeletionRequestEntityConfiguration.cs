using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using ServerAPIApp.Domain.Constants;
using ServerAPIApp.Domain.Entities;

namespace ServerAPIApp.DAL.Confs
{
    public class ProblemDeletionRequestEntityConfiguration : IEntityTypeConfiguration<ProblemDeletionRequestEntity>
    {
        public void Configure(EntityTypeBuilder<ProblemDeletionRequestEntity> builder)
        {
            builder.ToTable("problem_deletion_requests");

            builder
                .Property(x => x.Id)
                .IsRequired()
                .HasColumnName("id");

            builder.HasKey(x => x.Id);

            builder
                .Property(x => x.Reason)
                .IsRequired()
                .HasMaxLength(ApplicationConstants.DeletionRequestReasonMaxLength)
                .HasColumnName("reason");

            builder
                .Property(x => x.ProblemId)
                .IsRequired()
                .HasColumnName("problem_id");

            builder.HasOne(x => x.Initiator)
                .WithMany()
                .HasForeignKey(x => x.InitiatorId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.HasOne(x => x.Problem)
                .WithMany()
                .HasForeignKey(x => x.ProblemId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
