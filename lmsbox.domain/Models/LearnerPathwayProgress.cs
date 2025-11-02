using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace lmsbox.domain.Models;

public class LearnerPathwayProgress
{
    [Key]
    public int Id { get; set; }

    // User relationship
    [Required]
    public string UserId { get; set; } = null!;
    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }

    // Learning pathway relationship
    [Required]
    public string LearningPathwayId { get; set; } = null!;
    [ForeignKey(nameof(LearningPathwayId))]
    public LearningPathway? LearningPathway { get; set; }

    // Progress tracking
    public int CompletedCourses { get; set; } = 0;
    public int TotalCourses { get; set; } = 0;
    public int ProgressPercent { get; set; } = 0; // 0-100

    public bool IsCompleted { get; set; } = false;
    public DateTime? CompletedAt { get; set; }

    // Enrollment tracking
    public DateTime EnrolledAt { get; set; } = DateTime.UtcNow;
    public DateTime? LastAccessedAt { get; set; }

    // Current course in pathway
    public string? CurrentCourseId { get; set; }
    [ForeignKey(nameof(CurrentCourseId))]
    public Course? CurrentCourse { get; set; }
}