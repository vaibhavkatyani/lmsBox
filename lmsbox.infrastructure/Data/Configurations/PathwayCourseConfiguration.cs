using lmsbox.domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace lmsbox.infrastructure.Data.Configurations;

public class PathwayCourseConfiguration : IEntityTypeConfiguration<PathwayCourse>
{
    public void Configure(EntityTypeBuilder<PathwayCourse> builder)
    {
        builder.HasKey(pc => pc.Id);

        builder.Property(pc => pc.IsMandatory)
            .HasDefaultValue(true);

        builder.Property(pc => pc.AddedAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Relationships
        builder.HasOne(pc => pc.LearningPathway)
            .WithMany(lp => lp.PathwayCourses)
            .HasForeignKey(pc => pc.LearningPathwayId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(pc => pc.Course)
            .WithMany()
            .HasForeignKey(pc => pc.CourseId)
            .OnDelete(DeleteBehavior.Cascade);

        // Indexes for performance
        builder.HasIndex(pc => pc.LearningPathwayId);
        builder.HasIndex(pc => pc.CourseId);
        builder.HasIndex(pc => new { pc.LearningPathwayId, pc.SequenceOrder });

        // Unique constraint: each course can only appear once in a pathway
        builder.HasIndex(pc => new { pc.LearningPathwayId, pc.CourseId })
            .IsUnique();
    }
}