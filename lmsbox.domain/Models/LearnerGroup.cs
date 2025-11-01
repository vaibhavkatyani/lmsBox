using System;
using System.ComponentModel.DataAnnotations.Schema;

namespace lmsbox.domain.Models;
public class LearnerGroup
{
    public int Id { get; set; }

    // User (Identity key is string)
    public string UserId { get; set; } = null!;
    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }

    // LearningGroup
    public long LearningGroupId { get; set; }
    [ForeignKey(nameof(LearningGroupId))]
    public LearningGroup? LearningGroup { get; set; }

    public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    public bool IsActive { get; set; } = true;
}