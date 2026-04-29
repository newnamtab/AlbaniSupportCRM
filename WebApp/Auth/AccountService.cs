using Microsoft.AspNetCore.Components;
using System.Net;
using System.Net.Http.Json;
using WebApp.Models;

namespace WebApp.Auth
{

    /// <summary>
    /// Client-side authentication service for Blazor WASM.
    /// Manages user login/logout/registration with JWT tokens stored in HTTP-only cookies.
    /// Token is NOT accessed directly from JavaScript - cookies handle this securely.
    /// </summary>
    public interface IAccountService
    {
        /// <summary>Currently authenticated user</summary>
        UserProfile? User { get; }

        /// <summary>Is user currently authenticated</summary>
        bool IsAuthenticated { get; }

        /// <summary>Currently decoded JWT claims</summary>
        JwtTokenClaims? TokenClaims { get; }

        /// <summary>Initialize service and restore session if valid</summary>
        Task InitializeAsync();

        /// <summary>Login with email and password</summary>
        Task<AuthResult> LoginAsync(Login model);

        /// <summary>Register new user account</summary>
        Task<AuthResult> RegisterAsync(AddUser model);

        /// <summary>Logout and clear session</summary>
        Task LogoutAsync();

        /// <summary>Refresh JWT token before expiry</summary>
        Task<AuthResult> RefreshTokenAsync();

        /// <summary>Force refresh token on demand</summary>
        Task ForceTokenRefreshAsync();
    }

    /// <summary>Login response from API</summary>
    public class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; } // seconds
        public UserProfile User { get; set; } = UserProfile.Empty;
    }
    public class AuthResult
    {
        public bool Success { get; }
        public string Message { get; } = string.Empty;
        public IEnumerable<string> Errors { get; }

        private AuthResult(bool success, string message, IEnumerable<string> errors)
        {
            Success = success;
            Message = message;
            Errors = errors;
        }
        public static AuthResult SuccessResult(string message = "Operation successful") =>
            new AuthResult(true, message, Array.Empty<string>());
        public static AuthResult FailureResult(string message = "Operation failed", IEnumerable<string> errors = null) =>
            new AuthResult(false, message, errors ?? Array.Empty<string>());
    }

    /// <summary>Register response from API</summary>
    public class RegisterResponse
    {
        public string Message { get; set; } = string.Empty;
        public User User { get; set; } = User.Empty;
    }

    /// <summary>Token refresh response from API</summary>
    public class RefreshTokenResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; }
    }

    public class AccountService : IAccountService
    {
        private readonly HttpClient _httpClient;
        private readonly NavigationManager _navigationManager;
        private readonly ILogger<AccountService> _logger;

        private UserProfile? _currentUser;
        private JwtTokenClaims? _tokenClaims;
        private System.Timers.Timer? _tokenRefreshTimer;

        private const int TOKEN_REFRESH_BUFFER_SECONDS = 60; // Refresh 1 min before expiry
        private const int MIN_REFRESH_INTERVAL_SECONDS = 5;

        public UserProfile? User => _currentUser;
        public bool IsAuthenticated => _currentUser != null && _tokenClaims != null && !_tokenClaims.IsExpired;
        public JwtTokenClaims? TokenClaims => _tokenClaims;

        public AccountService(
            IHttpClientFactory httpClientFactory,
            NavigationManager navigationManager,
            ILogger<AccountService> logger
        )
        {
            _httpClient = httpClientFactory.CreateClient("API");
            _navigationManager = navigationManager;
            _logger = logger;
        }

        /// <summary>Initialize service and attempt to restore session</summary>
        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Initializing AccountService");

                if (_currentUser != null)
                {
                    _logger.LogInformation($"Restored user session: {_currentUser.Email}");

                    // Attempt to refresh token to validate session
                    var refreshResult = await RefreshTokenAsync();
                    if (!refreshResult.Success)
                    {
                        _logger.LogWarning("Token refresh failed during initialization");
                        await LogoutAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during AccountService initialization");
                _currentUser = null;
                _tokenClaims = null;
            }
        }

        /// <summary>Login user with credentials</summary>
        public async Task<AuthResult> LoginAsync(Login model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
                {
                    return AuthResult.FailureResult("Email and password are required", new List<string> { "Validation failed" });
                }

                _logger.LogInformation($"Login attempt for: {model.Email}");

                var response = await _httpClient.PostAsJsonAsync("api/auth/login", model);

                if (!response.IsSuccessStatusCode)
                {
                    return await HandleErrorResponseAsync(response);
                }

                var result = await response.Content.ReadAsAsync<LoginResponse>();

                if (result.User != null)
                {
                    // Set user and decode token claims from JWT in cookie
                    _currentUser = result.User;

                    // Build token claims from the response data
                    // Note: Token is in HTTP-only cookie and not accessible from client code
                    // We construct JwtTokenClaims from the returned user data for UI purposes
                    _tokenClaims = new JwtTokenClaims
                    {
                        UserId = result.User.Id,
                        Email = result.User.Email,
                        FirstName = result.User.FirstName,
                        LastName = result.User.LastName,
                        Role = result.User.Role,
                        IssuedAt = DateTime.UtcNow,
                        ExpiresAt = DateTime.UtcNow.AddSeconds(result.ExpiresIn),
                        Jti = Guid.NewGuid().ToString() // Placeholder - actual JTI is in server token
                    };

                    // Start automatic token refresh timer
                    StartTokenRefreshTimer(result.ExpiresIn);

                    _logger.LogInformation($"Login successful for: {_currentUser.Email}");
                }
                else
                {
                    _logger.LogWarning($"Login failed:");
                }

                return AuthResult.SuccessResult("Login successful");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return AuthResult.FailureResult("An error occurred during login", new List<string> { ex.Message }); 
            }
        }

        /// <summary>Register new user</summary>
        public async Task<AuthResult> RegisterAsync(AddUser model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
                {
                    return AuthResult.FailureResult("Email and password are required", new List<string> { "Validation failed" });
                }

                _logger.LogInformation($"Registration attempt for: {model.Email}");

                var response = await _httpClient.PostAsJsonAsync("/api/auth/register", model);

                if (!response.IsSuccessStatusCode)
                {
                    return await HandleErrorResponseAsync(response);
                }

                var result = await response.Content.ReadAsAsync<RegisterResponse>();

                if (result != null)
                {
                    _logger.LogInformation($"Registration successful for: {model.Email}");
                    return AuthResult.SuccessResult(result.Message);
                }

                _logger.LogWarning($"Registration failed: {result.Message}");
                return AuthResult.FailureResult("Registration failed", new List<string> { result?.Message ?? "Unknown error" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return AuthResult.FailureResult("An error occurred during registration", new List<string> { ex.Message });
            }
        }

        /// <summary>Refresh JWT token before expiry</summary>
        public async Task<AuthResult> RefreshTokenAsync()
        {
            try
            {
                _logger.LogInformation("Refreshing JWT token");

                // Token is in HTTP-only cookie, automatically sent by browser
                var response = await _httpClient.PostAsync("/api/auth/refresh-token", null);

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        _logger.LogWarning("Token refresh returned 401 - session expired");
                        await LogoutAsync();
                    }

                    return await HandleErrorResponseAsync(response);
                }

                var result = await response.Content.ReadAsAsync<RefreshTokenResponse>();

                if (result != null)
                {
                    _logger.LogInformation("Token refreshed successfully");

                    // Update token expiry time
                    if (_tokenClaims != null)
                    {
                        _tokenClaims.ExpiresAt = DateTime.UtcNow.AddSeconds(result.ExpiresIn);
                    }

                    // Restart refresh timer with new expiry
                    StopTokenRefreshTimer();
                    StartTokenRefreshTimer(result.ExpiresIn);
                }

                return AuthResult.SuccessResult("Token refreshed successfully");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return AuthResult.FailureResult("Token refresh failed", new List<string> { ex.Message });
            }
        }

        /// <summary>Logout and clear session</summary>
        public async Task LogoutAsync()
        {
            try
            {
                _logger.LogInformation("Logging out user");

                StopTokenRefreshTimer();

                // Notify API to invalidate tokens
                try
                {
                    await _httpClient.PostAsync("/api/auth/logout", null);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error notifying API of logout (non-critical)");
                }

                // Clear local state
                _currentUser = null;
                _tokenClaims = null;

                _logger.LogInformation("User logged out successfully");
                _navigationManager.NavigateTo("/");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
            }
        }

        /// <summary>Force token refresh on demand</summary>
        public async Task ForceTokenRefreshAsync()
        {
            _logger.LogInformation("Force refreshing token on demand");
            await RefreshTokenAsync();
        }

        /// <summary>Start automatic token refresh timer</summary>
        private void StartTokenRefreshTimer(int expiresInSeconds)
        {
            StopTokenRefreshTimer();

            // Calculate refresh time: 1 minute before expiry, minimum 5 seconds
            int refreshDelayMs = Math.Max(
                MIN_REFRESH_INTERVAL_SECONDS * 1000,
                (expiresInSeconds - TOKEN_REFRESH_BUFFER_SECONDS) * 1000
            );

            _tokenRefreshTimer = new System.Timers.Timer(refreshDelayMs);
            _tokenRefreshTimer.Elapsed += async (sender, e) =>
            {
                await RefreshTokenAsync();
            };
            _tokenRefreshTimer.AutoReset = false;
            _tokenRefreshTimer.Start();

            _logger.LogInformation($"Token refresh timer started for {refreshDelayMs}ms");
        }

        /// <summary>Stop token refresh timer</summary>
        private void StopTokenRefreshTimer()
        {
            if (_tokenRefreshTimer != null)
            {
                _tokenRefreshTimer.Stop();
                _tokenRefreshTimer.Dispose();
                _tokenRefreshTimer = null;
                _logger.LogInformation("Token refresh timer stopped");
            }
        }

        /// <summary>Handle API error responses</summary>
        private async Task<AuthResult> HandleErrorResponseAsync(HttpResponseMessage response) =>
         AuthResult.FailureResult($"HTTP {response.StatusCode}: {response.ReasonPhrase}",
                                  new List<string> { response.ReasonPhrase ?? "Unknown error" });
    }
}
