using AlbaniSupportCRM.settings;
using API.Auth;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace API.Services
{
    /// <summary>
    /// Service for generating and validating JWT tokens.
    /// </summary>
    public interface ITokenService
    {
        /// <summary>Generates a new JWT access token</summary>
        string GenerateAccessToken(UserProfile user);

        /// <summary>Generates a refresh token for token rotation</summary>
        string GenerateRefreshToken(Guid userId);

        /// <summary>Validates a refresh token and returns the user ID if valid</summary>
        Guid? ValidateRefreshToken(string token);

        /// <summary>Gets the access token expiry time in seconds</summary>
        int TokenExpirySeconds { get; }
    }

    public class TokenService : ITokenService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly ILogger<TokenService> _logger;

        public TokenService(IOptions<JwtSettings> configuration, ILogger<TokenService> logger)
        {
            _jwtSettings = configuration.Value;
            _logger = logger;
        }

        /// <summary>
        /// Generates a JWT access token with user claims.
        /// </summary>
        public string GenerateAccessToken(UserProfile user)
        {
            var secretKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(_jwtSettings.SecretKey ?? throw new InvalidOperationException("JWT SecretKey not configured"))
            );

            var credentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.GivenName, user.FirstName),
                new Claim(ClaimTypes.Surname, user.LastName),
                new Claim(ClaimTypes.Role, user.Role ?? "User"),
                new Claim("jti", Guid.NewGuid().ToString()) // JWT ID for token tracking
            };

            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes( _jwtSettings.ExpiryMinutes ),
                signingCredentials: credentials
            );

            var tokenHandler = new JwtSecurityTokenHandler();
            return tokenHandler.WriteToken(token);
        }

        /// <summary>
        /// Generates a secure refresh token (random bytes encoded as base64).
        /// </summary>
        public string GenerateRefreshToken(Guid userId)
        {
            var randomNumber = new byte[64];
            using (var rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
            }

            return Convert.ToBase64String(randomNumber);
        }

        /// <summary>
        /// Validates a refresh token. In production, validate against database.
        /// </summary>
        public Guid? ValidateRefreshToken(string token)
        {
            try
            {
                // In production, verify the token exists in the database
                // and hasn't been revoked
                var secretKey = new SymmetricSecurityKey(
                    Encoding.UTF8.GetBytes(_jwtSettings.SecretKey ?? throw new InvalidOperationException())
                );

                var tokenHandler = new JwtSecurityTokenHandler();
                tokenHandler.ValidateToken(token, new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = secretKey,
                    ValidateIssuer = true,
                    ValidIssuer = _jwtSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = _jwtSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                }, out SecurityToken validatedToken);

                var jwtToken = (JwtSecurityToken)validatedToken;
                var userIdClaim = jwtToken.Claims.FirstOrDefault(x => x.Type == ClaimTypes.NameIdentifier);

                if (userIdClaim != null && Guid.TryParse(userIdClaim.Value, out var userId))
                {
                    return userId;
                }

                return null;
            }
            catch (Exception ex)
            {
                _logger.LogWarning($"Token validation failed: {ex.Message}");
                return null;
            }
        }

        /// <summary>Gets the access token expiry time in seconds</summary>
        public int TokenExpirySeconds => _jwtSettings.ExpiryMinutes * 60;
    }
}
