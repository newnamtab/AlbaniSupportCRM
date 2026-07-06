namespace API.Auth.Responses
{
    public record RegisterResponse(Guid UserId);
    public record LoginResponse(Guid UserId, string Email, string FirstName, string LastName, int ExpiresIn);
    public record RefreshTokenResponse(int ExpiresIn);

    /// <summary>JWT token claims decoded from token</summary>
    public class JwtTokenClaims
    {
        public int UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Role { get; set; }
        public DateTime IssuedAt { get; set; }
        public DateTime ExpiresAt { get; set; }
        public string Jti { get; set; } = string.Empty; // JWT ID for tracking

        public bool IsExpired => DateTime.UtcNow >= ExpiresAt;
        public TimeSpan TimeUntilExpiry => ExpiresAt - DateTime.UtcNow;
    }
}
