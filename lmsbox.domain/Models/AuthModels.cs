namespace lmsbox.domain.Models
{
    public class LoginRequest
    {
        public required string Username { get; set; }
        public required string Password { get; set; }
    }

    public record LoginResponse(string Token);
}
