using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace lmsbox.domain.Models;
public class Lesson
{
    public long Id { get; set; }

    public long CourseId { get; set; }
    [ForeignKey(nameof(CourseId))]
    public Course? Course { get; set; }

    [Required]
    public string Title { get; set; } = null!;

    public string? Content { get; set; }

    public int Ordinal { get; set; }

    // Who created the lesson (organisation admin)
    public string CreatedByUserId { get; set; } = null!;
    [ForeignKey(nameof(CreatedByUserId))]
    public ApplicationUser? CreatedByUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}