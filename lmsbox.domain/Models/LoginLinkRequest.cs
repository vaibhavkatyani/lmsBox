namespace lmsbox.domain.Models
{
    public class LoginLinkRequest
    {
        public required string Email { get; set; }

        // Optional: token returned by client-side reCAPTCHA execution
        public string? RecaptchaToken { get; set; }
    }
    public class VerifyLoginLinkRequest
    {
        public string Token { get; set; } = string.Empty;
    }
}