using System;

namespace lmsbox.domain.Models;
public class Badge
{
    public int Id { get; set; }
    public string Name { get; set; } = null!;
    public string? Description { get; set; }
    public DateTime AwardedAt { get; set; } = DateTime.UtcNow;

    // owner
    public string? UserId { get; set; }
    public ApplicationUser? User { get; set; }
}