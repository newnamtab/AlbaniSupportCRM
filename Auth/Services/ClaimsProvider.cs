using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace Auth.Services
{
    internal static class ClaimsProvider
    {
        public static IEnumerable<Claim> ClaimsForUser(Guid userId,
                                                       string email,
                                                       string firstName,
                                                       string lastName,
                                                       string? role) =>
                                                       new List<Claim>
                                                       {
                                                           new Claim(JwtRegisteredClaimNames.Sub, userId.ToString()),
                                                           new Claim(JwtRegisteredClaimNames.Email, email),
                                                           new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()), // JWT ID for token tracking
                                                           new Claim(ClaimTypes.Role, role ?? "User"),
                                                           new Claim(CustomClaimTypes.FirstName, firstName),
                                                           new Claim(CustomClaimTypes.LastName, lastName),
                                                           };
    }
    internal record CustomClaimTypes
    {
        public const string FirstName = "firstName";
        public const string LastName = "lastName";
    }

}
