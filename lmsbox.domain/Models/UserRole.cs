using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace lmsbox.domain.Models;
public class UserRole
{
    public int Id { get; set; }

    [Required]
    public string Name { get; set; } = null!;

    public string? Description { get; set; }

    // Navigation to join entities
    public ICollection<UserUserRole> UserUserRoles { get; set; } = new List<UserUserRole>();
}