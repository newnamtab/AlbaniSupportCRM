using Auth.Settings;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Storage.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace Auth.Services
{
    public interface IJwtTokenService
    {
        Task<TokenResponse> GenerateTokensAsync(ASMemberUser user);
        string GenerateRefreshToken();
    }
    internal class JwtTokenService : IJwtTokenService
    {
        private readonly JwtSettings _jwtSettings;
        private readonly UserManager<ASMemberUser> _userManager;

        public JwtTokenService(IOptions<JwtSettings> jwtOptions, UserManager<ASMemberUser> userManager)
        {
            _jwtSettings = jwtOptions.Value;
            _userManager = userManager;
        }
        public async Task<TokenResponse> GenerateTokensAsync(ASMemberUser user)
        {
            var secretKey = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);
            
            var roles = await _userManager.GetRolesAsync(user);
            var claims = ClaimsProvider.ClaimsForUser(Guid.Parse(user.Id), user.Email ?? "", user.FirstName, user.LastName, roles.FirstOrDefault() );
            //claims.AddRange(roles.Select(role => new Claim(ClaimTypes.Role, role)));
            
            var key = new SymmetricSecurityKey(secretKey);
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: _jwtSettings.Issuer,
                audience: _jwtSettings.Audience,
                claims: claims,
                expires: DateTime.Now.AddMinutes(_jwtSettings.ExpiryMinutes),
                signingCredentials: credentials);
            
            var refreshToken = GenerateRefreshToken();
            user.RefreshToken = refreshToken;
            user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
            await _userManager.UpdateAsync(user);
            
            return new TokenResponse(
                new JwtSecurityTokenHandler().WriteToken(token),
                refreshToken,
                _jwtSettings.ExpiryMinutes * 60,
                roles.FirstOrDefault() );
        }
        private ClaimsPrincipal? GetPrincipalFromExpiredToken(string token)
        {
            var secretKey = Encoding.UTF8.GetBytes(_jwtSettings.SecretKey);
            var tokenValidationParameters = new TokenValidationParameters
            {
                ValidateAudience = false,
                ValidateIssuer = false,
                ValidateIssuerSigningKey = true,
                IssuerSigningKey = new SymmetricSecurityKey(secretKey),
                ValidateLifetime = false // Allow expired tokens
            };
            var tokenHandler = new JwtSecurityTokenHandler();
            var principal = tokenHandler.ValidateToken(token, tokenValidationParameters, out var securityToken);
            if (securityToken is not JwtSecurityToken jwtSecurityToken ||
                !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token");
            }
            return principal;
        }
        public string GenerateRefreshToken()
        {
            var randomNumber = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(randomNumber);
            return Convert.ToBase64String(randomNumber);
        }
    }
    public record TokenResponse(
        string AccessToken,
        string RefreshToken,
        int ExpiresIn,
        string? Role);
}
