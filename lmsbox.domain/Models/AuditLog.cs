using System;

namespace lmsbox.domain.Models;
public class AuditLog
{
    public long Id { get; set; }
    public string Action { get; set; } = null!;
    public string PerformedBy { get; set; } = string.Empty;
    public DateTime PerformedAt { get; set; } = DateTime.UtcNow;
    public string? Details { get; set; }
}