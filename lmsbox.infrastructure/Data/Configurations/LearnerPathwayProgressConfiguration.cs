using lmsbox.domain.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace lmsbox.infrastructure.Data.Configurations;

public class LearnerPathwayProgressConfiguration : IEntityTypeConfiguration<LearnerPathwayProgress>
{
    public void Configure(EntityTypeBuilder<LearnerPathwayProgress> builder)
    {
        builder.HasKey(lpp => lpp.Id);

        builder.Property(lpp => lpp.CompletedCourses)
            .HasDefaultValue(0);

        builder.Property(lpp => lpp.TotalCourses)
            .HasDefaultValue(0);

        builder.Property(lpp => lpp.ProgressPercent)
            .HasDefaultValue(0);

        builder.Property(lpp => lpp.IsCompleted)
            .HasDefaultValue(false);

        builder.Property(lpp => lpp.EnrolledAt)
            .HasDefaultValueSql("GETUTCDATE()");

        // Relationships
        builder.HasOne(lpp => lpp.User)
            .WithMany()
            .HasForeignKey(lpp => lpp.UserId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(lpp => lpp.LearningPathway)
            .WithMany(lp => lp.LearnerProgresses)
            .HasForeignKey(lpp => lpp.LearningPathwayId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(lpp => lpp.CurrentCourse)
            .WithMany()
            .HasForeignKey(lpp => lpp.CurrentCourseId)
            .OnDelete(DeleteBehavior.SetNull);

        // Indexes for performance
        builder.HasIndex(lpp => lpp.UserId);
        builder.HasIndex(lpp => lpp.LearningPathwayId);
        builder.HasIndex(lpp => new { lpp.UserId, lpp.LearningPathwayId });
        builder.HasIndex(lpp => lpp.EnrolledAt);

        // Unique constraint: one progress record per user per pathway
        builder.HasIndex(lpp => new { lpp.UserId, lpp.LearningPathwayId })
            .IsUnique();
    }
}