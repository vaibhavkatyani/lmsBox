using lmsbox.domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace lmsbox.infrastructure.Data.Configurations;

public class LearningPathwayConfiguration : IEntityTypeConfiguration<LearningPathway>
{
    public void Configure(EntityTypeBuilder<LearningPathway> builder)
    {
        builder.HasKey(lp => lp.Id);

        builder.Property(lp => lp.Title)
            .IsRequired()
            .HasMaxLength(250);

        builder.Property(lp => lp.DifficultyLevel)
            .HasDefaultValue("Beginner");

        builder.Property(lp => lp.IsActive)
            .HasDefaultValue(true);

        builder.Property(lp => lp.CreatedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Relationships
        builder.HasOne(lp => lp.Organisation)
            .WithMany()
            .HasForeignKey(lp => lp.OrganisationId)
            .OnDelete(DeleteBehavior.NoAction);

        builder.HasOne(lp => lp.CreatedByUser)
            .WithMany()
            .HasForeignKey(lp => lp.CreatedByUserId)
            .OnDelete(DeleteBehavior.NoAction);

        // Index for performance
        builder.HasIndex(lp => lp.OrganisationId);
        builder.HasIndex(lp => lp.CreatedAt);
        builder.HasIndex(lp => new { lp.OrganisationId, lp.IsActive });
    }
}