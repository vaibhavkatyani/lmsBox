using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using lmsbox.domain.Models;

namespace lmsbox.infrastructure.Data.Configurations;
public class LearningGroupConfiguration : IEntityTypeConfiguration<LearningGroup>
{
    public void Configure(EntityTypeBuilder<LearningGroup> builder)
    {
        builder.ToTable("LearningGroups");

        builder.HasKey(lg => lg.Id);

        builder.Property(lg => lg.Name).IsRequired().HasMaxLength(200);

        // FK -> CreatedByUser: DO NOT cascade
        builder.HasOne(lg => lg.CreatedByUser)
               .WithMany()
               .HasForeignKey(lg => lg.CreatedByUserId)
               .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(lg => lg.Organisation)
               .WithMany()
               .HasForeignKey(lg => lg.OrganisationId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}