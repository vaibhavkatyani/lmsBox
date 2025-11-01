using System;
using System.ComponentModel.DataAnnotations;

namespace lmsbox.infrastructure.Data
{
    public class RevokedToken
    {
        [Key]
        public Guid Id { get; set; }

        // JWT "jti" claim value if available
        public string? Jti { get; set; }

        // Optional SHA256 hash of raw token (used if jti missing or for extra safety)
        public string? TokenHash { get; set; }

        // When the token was revoked
        public DateTime RevokedAt { get; set; }

        // When the token naturally expires — can be used to purge old revoked entries
        public DateTime ExpiresAt { get; set; }
    }
}