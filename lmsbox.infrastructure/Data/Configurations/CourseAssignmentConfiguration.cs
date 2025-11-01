using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using lmsbox.domain.Models;

namespace lmsbox.infrastructure.Data.Configurations;
public class CourseAssignmentConfiguration : IEntityTypeConfiguration<CourseAssignment>
{
    public void Configure(EntityTypeBuilder<CourseAssignment> builder)
    {
        builder.ToTable("CourseAssignments");

        builder.HasKey(ca => ca.Id);

        builder.HasOne(ca => ca.Course)
               .WithMany(c => c.CourseAssignments)
               .HasForeignKey(ca => ca.CourseId)
               .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(ca => ca.LearningGroup)
               .WithMany()
               .HasForeignKey(ca => ca.LearningGroupId)
               .OnDelete(DeleteBehavior.Cascade);

        // AssignedByUser -> DO NOT cascade
        builder.HasOne(ca => ca.AssignedByUser)
               .WithMany()
               .HasForeignKey(ca => ca.AssignedByUserId)
               .OnDelete(DeleteBehavior.Restrict);
    }
}