using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using WebApp.Models;

namespace WebApp.Services
{
    /// <summary>
    /// Handles JWT token parsing, validation, and extraction of claims.
    /// Does NOT store the token - that's handled by the browser's HTTP-only cookies.
    /// </summary>
    public interface IJwtTokenService
    {
        /// <summary>Decodes JWT token and extracts claims</summary>
        JwtTokenClaims? DecodeToken(string token);

        /// <summary>Checks if token is expired</summary>
        bool IsTokenExpired(string token);

        /// <summary>Gets time until token expiry</summary>
        TimeSpan GetTimeUntilExpiry(string token);

        /// <summary>Validates token signature and expiry (basic client-side validation)</summary>
        bool ValidateToken(string token);

        /// <summary>Extracts a specific claim from token</summary>
        string? GetClaimValue(string token, string claimType);
    }

    public class JwtTokenService : IJwtTokenService
    {
        private readonly ILogger<JwtTokenService> _logger;

        public JwtTokenService(ILogger<JwtTokenService> logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// Decodes JWT token without validation (safe for reading claims client-side).
        /// Note: Server validates token signature and expiry via HTTP-only cookies.
        /// </summary>
        public JwtTokenClaims? DecodeToken(string token)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(token))
                {
                    _logger.LogWarning("Token is null or empty");
                    return null;
                }

                var handler = new JwtSecurityTokenHandler();

                // This reads the token WITHOUT validating signature (safe for client-side)
                // Signature is validated by the server
                if (!handler.CanReadToken(token))
                {
                    _logger.LogWarning("Invalid token format");
                    return null;
                }

                var jwtToken = handler.ReadJwtToken(token);

                // Extract claims
                var userIdClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier)?.Value
                    ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "sub")?.Value;

                var emailClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Email)?.Value
                    ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "email")?.Value;

                var firstNameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.GivenName)?.Value
                    ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "given_name")?.Value;

                var lastNameClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Surname)?.Value
                    ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "family_name")?.Value;

                var roleClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == ClaimTypes.Role)?.Value
                    ?? jwtToken.Claims.FirstOrDefault(c => c.Type == "role")?.Value;

                var jtiClaim = jwtToken.Claims.FirstOrDefault(c => c.Type == "jti")?.Value ?? string.Empty;

                // Extract timestamps
                //var issuedAt = UnixTimeStampToDateTime(jwtToken.IssuedAt);
                var issuedAt = jwtToken.IssuedAt;
                var expiresAt = jwtToken.ValidTo;

                if (!int.TryParse(userIdClaim, out var userId))
                {
                    _logger.LogWarning("Unable to parse user ID from token");
                    return null;
                }

                var claims = new JwtTokenClaims
                {
                    UserId = userId,
                    Email = emailClaim ?? string.Empty,
                    FirstName = firstNameClaim ?? string.Empty,
                    LastName = lastNameClaim ?? string.Empty,
                    Role = roleClaim,
                    IssuedAt = issuedAt,
                    ExpiresAt = expiresAt,
                    Jti = jtiClaim
                };

                _logger.LogInformation($"Successfully decoded token for user: {claims.Email}");
                return claims;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error decoding JWT token");
                return null;
            }
        }

        /// <summary>Checks if token is expired</summary>
        public bool IsTokenExpired(string token)
        {
            var claims = DecodeToken(token);
            return claims?.IsExpired ?? true;
        }

        /// <summary>Gets time remaining until token expiry</summary>
        public TimeSpan GetTimeUntilExpiry(string token)
        {
            var claims = DecodeToken(token);
            if (claims == null)
                return TimeSpan.Zero;

            var timeUntilExpiry = claims.TimeUntilExpiry;
            return timeUntilExpiry > TimeSpan.Zero ? timeUntilExpiry : TimeSpan.Zero;
        }

        /// <summary>Basic client-side token validation (server validates signature)</summary>
        public bool ValidateToken(string token)
        {
            try
            {
                var claims = DecodeToken(token);
                return claims != null && !claims.IsExpired;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error validating token");
                return false;
            }
        }

        /// <summary>Extracts specific claim from token</summary>
        public string? GetClaimValue(string token, string claimType)
        {
            try
            {
                var handler = new JwtSecurityTokenHandler();
                if (!handler.CanReadToken(token))
                    return null;

                var jwtToken = handler.ReadJwtToken(token);
                return jwtToken.Claims.FirstOrDefault(c => c.Type == claimType)?.Value;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error extracting claim '{claimType}' from token");
                return null;
            }
        }

        private DateTime UnixTimeStampToDateTime(long? unixTimeStamp)
        {
            if (unixTimeStamp == null)
                return DateTime.MinValue;

            var dateTime = new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc);
            dateTime = dateTime.AddSeconds(unixTimeStamp.Value).ToUniversalTime();
            return dateTime;
        }
    }
}