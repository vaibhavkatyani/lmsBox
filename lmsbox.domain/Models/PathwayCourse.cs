using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace lmsbox.domain.Models;

public class PathwayCourse
{
    [Key]
    public int Id { get; set; }

    // Learning pathway relationship
    [Required]
    public string LearningPathwayId { get; set; } = null!;
    [ForeignKey(nameof(LearningPathwayId))]
    public LearningPathway? LearningPathway { get; set; }

    // Course relationship
    [Required]
    public string CourseId { get; set; } = null!;
    [ForeignKey(nameof(CourseId))]
    public Course? Course { get; set; }

    // Order in the pathway
    public int SequenceOrder { get; set; }

    // Whether this course is mandatory for pathway completion
    public bool IsMandatory { get; set; } = true;

    // Prerequisites within the pathway
    public string? PrerequisiteCourseIds { get; set; } // JSON array of course IDs

    public DateTime AddedAt { get; set; } = DateTime.UtcNow;
}