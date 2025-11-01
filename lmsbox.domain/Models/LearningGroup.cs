using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace lmsbox.domain.Models;
public class LearningGroup
{
    public long Id { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    // Ownership: group belongs to an organisation
    public long OrganisationId { get; set; }
    [ForeignKey(nameof(OrganisationId))]
    public Organisation? Organisation { get; set; }

    // Creator (organisation admin)
    public string CreatedByUserId { get; set; } = null!;
    [ForeignKey(nameof(CreatedByUserId))]
    public ApplicationUser? CreatedByUser { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    // Members (many-to-many via LearnerGroup join entity)
    public ICollection<LearnerGroup> LearnerGroups { get; set; } = new List<LearnerGroup>();

    // Courses mapped to this group
    public ICollection<GroupCourse> GroupCourses { get; set; } = new List<GroupCourse>();
}