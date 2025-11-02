using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace lmsbox.domain.Models;

public class LearningPathway
{
    [Key]
    [DatabaseGenerated(DatabaseGeneratedOption.None)]
    public string Id { get; set; } = null!;

    [Required]
    [MaxLength(250)]
    public string Title { get; set; } = null!;

    public string? Description { get; set; }

    public string? ShortDescription { get; set; }

    public string? Category { get; set; }

    public string? Tags { get; set; }

    public bool IsActive { get; set; } = true;

    public string? BannerUrl { get; set; }

    public int EstimatedDurationHours { get; set; }

    public string DifficultyLevel { get; set; } = "Beginner"; // Beginner, Intermediate, Advanced

    // Organization relationship
    public long OrganisationId { get; set; }
    [ForeignKey(nameof(OrganisationId))]
    public Organisation? Organisation { get; set; }

    // Creator relationship
    [Required]
    public string CreatedByUserId { get; set; } = null!;
    [ForeignKey(nameof(CreatedByUserId))]
    public ApplicationUser? CreatedByUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }

    // Navigation properties
    public virtual ICollection<PathwayCourse> PathwayCourses { get; set; } = new List<PathwayCourse>();
    public virtual ICollection<LearnerPathwayProgress> LearnerProgresses { get; set; } = new List<LearnerPathwayProgress>();
}