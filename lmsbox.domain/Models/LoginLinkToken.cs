using System;

namespace lmsbox.domain.Models;
public class LoginLinkToken
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string UserId { get; set; } = null!;
    public string TokenHash { get; set; } = null!;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime ExpiresAt { get; set; }
    public DateTime? UsedAt { get; set; }

    // Telemetry for email delivery
    public DateTime? SentAt { get; set; }
    public int SendFailedCount { get; set; }
    public string? LastSendError { get; set; }
}