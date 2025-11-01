using System.ComponentModel.DataAnnotations.Schema;

namespace lmsbox.domain.Models;
public class UserUserRole
{
    public int Id { get; set; }

    // FK to ApplicationUser (Identity key is string)
    public string UserId { get; set; } = null!;

    [ForeignKey(nameof(UserId))]
    public ApplicationUser? User { get; set; }

    // FK to custom UserRole
    public int RoleId { get; set; }

    [ForeignKey(nameof(RoleId))]
    public UserRole? Role { get; set; }

    public DateTime AssignedAt { get; set; } = DateTime.UtcNow;
    public string AssignedBy { get; set; } = string.Empty;
}