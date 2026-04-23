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
        Task<Result<LoginResponse>> LoginAsync(Login model);

        /// <summary>Register new user account</summary>
        Task<Result<RegisterResponse>> RegisterAsync(AddUser model);

        /// <summary>Logout and clear session</summary>
        Task LogoutAsync();

        /// <summary>Refresh JWT token before expiry</summary>
        Task<Result<RefreshTokenResponse>> RefreshTokenAsync();

        /// <summary>Get time until token expires</summary>
        TimeSpan GetTokenTimeRemaining();

        /// <summary>Force refresh token on demand</summary>
        Task ForceTokenRefreshAsync();
    }

    /// <summary>Generic API result wrapper</summary>
    public class Result<T>
    {
        public bool Success { get; set; }
        public T? Data { get; set; }
        public string? Message { get; set; }
        public List<string> Errors { get; set; } = new();
    }

    /// <summary>Login response from API</summary>
    public class LoginResponse
    {
        public string AccessToken { get; set; } = string.Empty;
        public int ExpiresIn { get; set; } // seconds
        public UserProfile User { get; set; } = UserProfile.Empty;
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

        private string _userKey = "user_profile";
        private string _tokenClaimsKey = "token_claims";

        private UserProfile? _currentUser;
        private JwtTokenClaims? _tokenClaims;
        private System.Timers.Timer? _tokenRefreshTimer;

        private const int TOKEN_REFRESH_BUFFER_SECONDS = 60; // Refresh 1 min before expiry
        private const int MIN_REFRESH_INTERVAL_SECONDS = 5;

        public UserProfile? User => _currentUser;
        public bool IsAuthenticated => _currentUser != null && _tokenClaims != null && !_tokenClaims.IsExpired;
        public JwtTokenClaims? TokenClaims => _tokenClaims;

        public AccountService(
            HttpClient httpClient,
            NavigationManager navigationManager,
            ILogger<AccountService> logger
        )
        {
            _httpClient = httpClient;
            _navigationManager = navigationManager;
            _logger = logger;
        }

        /// <summary>Initialize service and attempt to restore session</summary>
        public async Task InitializeAsync()
        {
            try
            {
                _logger.LogInformation("Initializing AccountService");

                // Restore user profile from localStorage
                _currentUser = null;

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
        public async Task<Result<LoginResponse>> LoginAsync(Login model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
                {
                    return new Result<LoginResponse>
                    {
                        Success = false,
                        Message = "Email and password are required",
                        Errors = new List<string> { "Validation failed" }
                    };
                }

                _logger.LogInformation($"Login attempt for: {model.Email}");

                var response = await _httpClient.PostAsJsonAsync("api/account/login", model);

                if (!response.IsSuccessStatusCode)
                {
                    return await HandleErrorResponseAsync<LoginResponse>(response);
                }

                var result = await response.Content.ReadAsAsync<Result<LoginResponse>>();

                if (result.Success && result.Data != null)
                {
                    // Set user and decode token claims from JWT in cookie
                    _currentUser = result.Data.User;
                    //await _localStorageService.SetItem(_userKey, _currentUser);

                    // Start automatic token refresh timer
                    StartTokenRefreshTimer(result.Data.ExpiresIn);

                    _logger.LogInformation($"Login successful for: {_currentUser.Email}");
                }
                else
                {
                    _logger.LogWarning($"Login failed: {result.Message}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return new Result<LoginResponse>
                {
                    Success = false,
                    Message = "An error occurred during login",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        /// <summary>Register new user</summary>
        public async Task<Result<RegisterResponse>> RegisterAsync(AddUser model)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(model.Email) || string.IsNullOrWhiteSpace(model.Password))
                {
                    return new Result<RegisterResponse>
                    {
                        Success = false,
                        Message = "Email and password are required",
                        Errors = new List<string> { "Validation failed" }
                    };
                }

                _logger.LogInformation($"Registration attempt for: {model.Email}");

                var response = await _httpClient.PostAsJsonAsync("api/account/register", model);

                if (!response.IsSuccessStatusCode)
                {
                    return await HandleErrorResponseAsync<RegisterResponse>(response);
                }

                var result = await response.Content.ReadAsAsync<Result<RegisterResponse>>();

                if (result.Success)
                {
                    _logger.LogInformation($"Registration successful for: {model.Email}");
                }
                else
                {
                    _logger.LogWarning($"Registration failed: {result.Message}");
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return new Result<RegisterResponse>
                {
                    Success = false,
                    Message = "An error occurred during registration",
                    Errors = new List<string> { ex.Message }
                };
            }
        }

        /// <summary>Refresh JWT token before expiry</summary>
        public async Task<Result<RefreshTokenResponse>> RefreshTokenAsync()
        {
            try
            {
                _logger.LogInformation("Refreshing JWT token");

                // Token is in HTTP-only cookie, automatically sent by browser
                var response = await _httpClient.PostAsync("api/account/refresh-token", null);

                if (!response.IsSuccessStatusCode)
                {
                    if (response.StatusCode == HttpStatusCode.Unauthorized)
                    {
                        _logger.LogWarning("Token refresh returned 401 - session expired");
                        await LogoutAsync();
                    }

                    return await HandleErrorResponseAsync<RefreshTokenResponse>(response);
                }

                var result = await response.Content.ReadAsAsync<Result<RefreshTokenResponse>>();

                if (result.Success && result.Data != null)
                {
                    _logger.LogInformation("Token refreshed successfully");

                    // Restart refresh timer with new expiry
                    StopTokenRefreshTimer();
                    StartTokenRefreshTimer(result.Data.ExpiresIn);
                }

                return result;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return new Result<RefreshTokenResponse>
                {
                    Success = false,
                    Message = "Token refresh failed",
                    Errors = new List<string> { ex.Message }
                };
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
                    await _httpClient.PostAsync("api/account/logout", null);
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error notifying API of logout (non-critical)");
                }

                // Clear local state
                _currentUser = null;
                _tokenClaims = null;

                //await _localStorageService.RemoveItem(_userKey);
                //await _localStorageService.RemoveItem(_tokenClaimsKey);

                _logger.LogInformation("User logged out successfully");
                _navigationManager.NavigateTo("/");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
            }
        }

        /// <summary>Get remaining time on token</summary>
        public TimeSpan GetTokenTimeRemaining()
        {
            return _tokenClaims?.TimeUntilExpiry ?? TimeSpan.Zero;
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
        private async Task<Result<T>> HandleErrorResponseAsync<T>(HttpResponseMessage response)
        {
            try
            {
                var result = await response.Content.ReadAsAsync<Result<T>>();
                return result;
            }
            catch
            {
                return new Result<T>
                {
                    Success = false,
                    Message = $"HTTP {response.StatusCode}: {response.ReasonPhrase}",
                    Errors = new List<string> { response.ReasonPhrase ?? "Unknown error" }
                };
            }
        }
    }
}
