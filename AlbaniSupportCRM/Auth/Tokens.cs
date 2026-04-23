namespace API.Auth
{
    public class TokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public string? RefreshToken { get; set; }
        public int ExpiresIn { get; set; } // seconds
        public string TokenType { get; set; } = "Bearer";
    }

    /// <summary>Login request model</summary>
    public class Login
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    /// <summary>Register request model</summary>
    public class AddUser
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }

    /// <summary>User profile model</summary>
    public class UserProfile
    {
        public Guid Id { get; set; }
        public string Email { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string? Role { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        public static UserProfile Empty => new UserProfile();

        public string FullName => $"{FirstName} {LastName}".Trim();
    }

    /// <summary>API response wrapper</summary>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }
    }

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
