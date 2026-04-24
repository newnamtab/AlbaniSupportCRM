using API.Auth;
using API.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AlbaniSupportCRM.API.Controllers
{
    /// <summary>
    /// Account management controller for user authentication and registration.
    /// Implements JWT token-based authentication with HTTP-only cookies.
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly IAuthenticationService _authenticationService;
        private readonly ITokenService _tokenService;
        private readonly IUserService _userService;
        private readonly ILogger<AccountController> _logger;
        private readonly IConfiguration _configuration;
        private const string JWT_COOKIE_NAME = "jwt_token";
        private const string REFRESH_COOKIE_NAME = "refresh_token";

        public AccountController(IAuthenticationService authenticationService, ITokenService tokenService, IUserService userService,
                                 ILogger<AccountController> logger,
                                 IConfiguration configuration)
        {
            _authenticationService = authenticationService;
            _tokenService = tokenService;
            _userService = userService;
            _logger = logger;
            _configuration = configuration;
        }

        /// <summary>
        /// Authenticates a user and returns tokens via HTTP-only cookies.
        /// Token is NOT returned in response body for security.
        /// </summary>
        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid request",
                    errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            try
            {
                _logger.LogInformation($"Login attempt for email: {request.Email}");

                // Validate user credentials
                var user = await _authenticationService.AuthenticateAsync(request.Email, request.Password);
                if (user == null)
                {
                    _logger.LogWarning($"Failed login attempt for email: {request.Email}");
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Invalid email or password"
                    });
                }

                // Generate JWT access token
                var accessToken = _tokenService.GenerateAccessToken(user);
                var accessTokenExpiry = _tokenService.GetTokenExpirySeconds();

                // Generate refresh token (optional but recommended)
                var refreshToken = _tokenService.GenerateRefreshToken(user.Id);
                await _authenticationService.StoreRefreshTokenAsync(user.Id, refreshToken);

                // Set HTTP-only cookies
                SetAuthCookies(accessToken, refreshToken, accessTokenExpiry);

                _logger.LogInformation($"Successful login for user: {user.Email}");

                // Return user info and token expiry (token itself is in the cookie)
                return Ok(new
                {
                    success = true,
                    message = "Login successful",
                    data = new
                    {
                        accessToken = string.Empty, // Token is in the cookie, not returned here
                        expiresIn = accessTokenExpiry,
                        user = new
                        {
                            id = user.Id,
                            email = user.Email,
                            firstName = user.FirstName,
                            lastName = user.LastName,
                            role = user.Role
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during login");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred during login",
                    errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Refreshes the JWT token using the refresh token stored in cookies.
        /// </summary>
        [HttpPost("refresh-token")]
        [AllowAnonymous] // Anonymous because cookie contains the refresh token
        public async Task<IActionResult> RefreshToken()
        {
            try
            {
                _logger.LogInformation("Token refresh request received");

                // Get refresh token from cookie
                if (!Request.Cookies.TryGetValue(REFRESH_COOKIE_NAME, out var refreshToken))
                {
                    _logger.LogWarning("Refresh token not found in cookies");
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Refresh token not found"
                    });
                }

                // Validate refresh token
                var userId = _tokenService.ValidateRefreshToken(refreshToken);
                if (userId == null)
                {
                    _logger.LogWarning("Invalid or expired refresh token");
                    return Unauthorized(new
                    {
                        success = false,
                        message = "Refresh token is invalid or expired"
                    });
                }

                // Get user
                var user = await _userService.GetUserByIdAsync(userId.Value);
                if (user == null)
                {
                    _logger.LogWarning($"User not found for token refresh: {userId}");
                    return Unauthorized(new
                    {
                        success = false,
                        message = "User not found"
                    });
                }

                // Generate new access token
                var newAccessToken = _tokenService.GenerateAccessToken(user);
                var accessTokenExpiry = _tokenService.GetTokenExpirySeconds();

                // Set new access token in cookie
                Response.Cookies.Append(
                    JWT_COOKIE_NAME,
                    newAccessToken,
                    GetCookieOptions(accessTokenExpiry)
                );

                _logger.LogInformation($"Token refreshed for user: {user.Email}");

                return Ok(new
                {
                    success = true,
                    message = "Token refreshed successfully",
                    data = new
                    {
                        accessToken = string.Empty, // Token is in the cookie
                        expiresIn = accessTokenExpiry
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during token refresh");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred during token refresh",
                    errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Registers a new user.
        /// </summary>
        [HttpPost("register")]
        [AllowAnonymous]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    success = false,
                    message = "Invalid request",
                    errors = ModelState.Values.SelectMany(v => v.Errors.Select(e => e.ErrorMessage))
                });
            }

            try
            {
                _logger.LogInformation($"Registration attempt for email: {request.Email}");

                // Check if user already exists
                var existingUser = await _userService.GetUserByEmailAsync(request.Email);
                if (existingUser != null)
                {
                    _logger.LogWarning($"Registration failed - email already registered: {request.Email}");
                    return BadRequest(new
                    {
                        success = false,
                        message = "Email is already registered"
                    });
                }

                // Create new user
                var user = await _userService.CreateUserAsync(
                    request.Email,
                    request.Password,
                    request.FirstName,
                    request.LastName
                );

                _logger.LogInformation($"User registered successfully: {user.Email}");

                return Ok(new
                {
                    success = true,
                    message = "Registration successful",
                    data = new
                    {
                        user = new
                        {
                            id = user.Id,
                            email = user.Email,
                            firstName = user.FirstName,
                            lastName = user.LastName,
                            role = user.Role
                        }
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during registration");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred during registration",
                    errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Logs out the user by clearing authentication cookies.
        /// </summary>
        [HttpPost("logout")]
        [Authorize] // User must be authenticated
        public IActionResult Logout()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                _logger.LogInformation($"Logout for user: {userId}");

                // Clear authentication cookies
                Response.Cookies.Delete(JWT_COOKIE_NAME);
                Response.Cookies.Delete(REFRESH_COOKIE_NAME);

                return Ok(new
                {
                    success = true,
                    message = "Logout successful"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during logout");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred during logout",
                    errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Gets the current user profile (requires authentication).
        /// </summary>
        [HttpGet("profile")]
        [Authorize]
        public async Task<IActionResult> GetProfile()
        {
            try
            {
                var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userId))
                {
                    return Unauthorized();
                }

                var user = await _userService.GetUserByIdAsync(Guid.Parse(userId));
                if (user == null)
                {
                    return NotFound();
                }

                return Ok(new
                {
                    success = true,
                    data = new
                    {
                        id = user.Id,
                        email = user.Email,
                        firstName = user.FirstName,
                        lastName = user.LastName,
                        role = user.Role
                    }
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error getting profile");
                return StatusCode(500, new
                {
                    success = false,
                    message = "An error occurred",
                    errors = new[] { ex.Message }
                });
            }
        }

        /// <summary>
        /// Sets HTTP-only cookies with authentication tokens.
        /// </summary>
        private void SetAuthCookies(string accessToken, string refreshToken, int expiresInSeconds)
        {
            // Access token cookie
            Response.Cookies.Append(
                JWT_COOKIE_NAME,
                accessToken,
                GetCookieOptions(expiresInSeconds)
            );

            // Refresh token cookie (longer expiry - typically 7 days)
            Response.Cookies.Append(
                REFRESH_COOKIE_NAME,
                refreshToken,
                GetCookieOptions(7 * 24 * 60 * 60) // 7 days
            );
        }

        /// <summary>
        /// Creates cookie options for HTTP-only, secure cookies.
        /// </summary>
        private CookieOptions GetCookieOptions(int expiresInSeconds)
        {
            return new CookieOptions
            {
                HttpOnly = true,           // Not accessible from JavaScript (prevents XSS)
                Secure = true,             // Only sent over HTTPS
                SameSite = SameSiteMode.Strict, // CSRF protection
                Expires = DateTime.UtcNow.AddSeconds(expiresInSeconds),
                IsEssential = true         // Allow even if user hasn't accepted cookies
            };
        }
    }

    // Request/Response DTOs
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class RegisterRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
    }
}

