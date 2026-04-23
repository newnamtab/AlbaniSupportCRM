using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;
using WebApp.Models;

namespace WebApp.Auth
{
    /// <summary>
    /// Custom authentication state provider for Blazor.
    /// Provides current user claims to Authorize components.
    /// </summary>
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly IAccountService _accountService;
        private readonly ILogger<CustomAuthenticationStateProvider> _logger;

        public CustomAuthenticationStateProvider(
            IAccountService accountService,
            ILogger<CustomAuthenticationStateProvider> logger
        )
        {
            _accountService = accountService;
            _logger = logger;
        }

        /// <summary>Gets current authentication state</summary>
        public override Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            try
            {
                var user = _accountService.User;
                var claims = _accountService.TokenClaims;

                if (user != null && claims != null && !claims.IsExpired)
                {
                    var claimsPrincipal = BuildClaimsPrincipal(user, claims);
                    var authState = new AuthenticationState(claimsPrincipal);
                    return Task.FromResult(authState);
                }

                var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
                return Task.FromResult(new AuthenticationState(anonymousUser));
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting authentication state");
                var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
                return Task.FromResult(new AuthenticationState(anonymousUser));
            }
        }

        /// <summary>Notify that authentication state has changed (after login/logout)</summary>
        public void NotifyUserAuthentication(UserProfile user, JwtTokenClaims claims)
        {
            var claimsPrincipal = BuildClaimsPrincipal(user, claims);
            var authState = Task.FromResult(new AuthenticationState(claimsPrincipal));
            NotifyAuthenticationStateChanged(authState);
            _logger.LogInformation($"User authenticated: {user.Email}");
        }

        /// <summary>Notify that user has logged out</summary>
        public void NotifyUserLogout()
        {
            var anonymousUser = new ClaimsPrincipal(new ClaimsIdentity());
            var authState = Task.FromResult(new AuthenticationState(anonymousUser));
            NotifyAuthenticationStateChanged(authState);
            _logger.LogInformation("User logged out");
        }

        /// <summary>Build ClaimsPrincipal from user and token claims</summary>
        private ClaimsPrincipal BuildClaimsPrincipal(UserProfile user, JwtTokenClaims claims)
        {
            var claimsIdentity = new ClaimsIdentity(BuildClaims(user, claims), "jwt");
            return new ClaimsPrincipal(claimsIdentity);
        }

        /// <summary>Build claims list for ClaimsPrincipal</summary>
        private List<Claim> BuildClaims(UserProfile user, JwtTokenClaims claims)
        {
            var claimsList = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.GivenName, user.FirstName),
                new Claim(ClaimTypes.Surname, user.LastName),
                new Claim("jti", claims.Jti)
            };

            if (!string.IsNullOrEmpty(user.Role))
            {
                claimsList.Add(new Claim(ClaimTypes.Role, user.Role));
            }

            return claimsList;
        }
    }
}
