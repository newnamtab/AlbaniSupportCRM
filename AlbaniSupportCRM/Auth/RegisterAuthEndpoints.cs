using API.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Auth
{
    public static class RegisterAuthEndpoints
    {
        private const string JWT_COOKIE_NAME = "jwt_token";
        private const string REFRESH_COOKIE_NAME = "refresh_token";

        public static IEndpointRouteBuilder Setup(this IEndpointRouteBuilder app)
        {
            // Setup authentication endpoints here
            app.MapPost("/api/auth/login", async Task<IResult> (
                                                  [FromServices] ILogger logger,
                                                  [FromServices] IAuthenticationService authenticationService,
                                                  [FromServices] ITokenService tokenService,
                                                  [FromServices] IHttpContextAccessor httpContextAccessor,
                                                  [FromBody] LoginRequest request) =>
            {
                try
                {
                    logger.LogInformation($"Login attempt for email: {request.Email}");
                    if (httpContextAccessor.HttpContext is null) return Results.Unauthorized();

                    // Validate user credentials
                    var user = await authenticationService.AuthenticateAsync(request.Email, request.Password);
                    if (user == null)
                    {
                        logger.LogWarning($"Failed login attempt for email: {request.Email}");
                        //"Invalid email or password" Return 401 Unauthorized for invalid credentials   
                        return Results.Unauthorized();
                    }

                    // Generate JWT access token
                    var accessToken = tokenService.GenerateAccessToken(user);
                    var accessTokenExpiry = tokenService.GetTokenExpirySeconds();

                    // Generate refresh token (optional but recommended)
                    var refreshToken = tokenService.GenerateRefreshToken(user.Id);
                    await authenticationService.StoreRefreshTokenAsync(user.Id, refreshToken);

                    // Set HTTP-only cookies
                    SetAuthCookies(accessToken, refreshToken, accessTokenExpiry, httpContextAccessor.HttpContext);

                    logger.LogInformation($"Successful login for user: {user.Email}");

                    // Return user info and token expiry (token itself is in the cookie)
                    return Results.Ok(new
                    {
                        id = user.Id,
                        email = user.Email,
                        firstName = user.FirstName,
                        lastName = user.LastName,
                        role = user.Role
                    });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error during login");
                    return Results.InternalServerError();
                }
            });

            app.MapPost("api/auth/refreshtoken)", async Task<IResult> (
                                                        [FromServices] ILogger logger,
                                                        [FromServices] ITokenService tokenService,
                                                        [FromServices] IUserService userService,
                                                        [FromServices] IHttpContextAccessor httpContextAccessor) =>
            {
                try
                {
                    logger.LogInformation("Token refresh request received");

                    // Get refresh token from cookie
                    if (httpContextAccessor.HttpContext!.Request.Cookies.TryGetValue(REFRESH_COOKIE_NAME, out var refreshToken) == false)
                    {
                        logger.LogWarning("Refresh token not found in cookies");
                        return Results.Unauthorized();
                    }

                    // Validate refresh token
                    var userId = tokenService.ValidateRefreshToken(refreshToken);
                    if (userId == null)
                    {
                        logger.LogWarning("Invalid or expired refresh token");
                        return Results.Unauthorized();
                    }

                    // Get user
                    var user = await userService.GetUserByIdAsync(userId.Value);
                    if (user == null)
                    {
                        logger.LogWarning($"User not found for token refresh: {userId}");
                        return Results.Unauthorized();
                    }

                    // Generate new access token
                    var newAccessToken = tokenService.GenerateAccessToken(user);
                    var accessTokenExpiry = tokenService.GetTokenExpirySeconds();

                    // Set new access token in cookie
                    httpContextAccessor.HttpContext!.Response.Cookies.Append(
                        JWT_COOKIE_NAME,
                        newAccessToken,
                        GetCookieOptions(accessTokenExpiry)
                    );

                    logger.LogInformation($"Token refreshed for user: {user.Email}");

                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error during token refresh");
                    return Results.InternalServerError();
                }
            });

            app.MapPost("/api/auth/register", async Task<IResult> ( [FromServices] ILogger logger,
                                                                    [FromServices] ITokenService tokenService,
                                                                    [FromServices] IUserService userService,
                                                                    [FromBody] RegisterRequest request) =>
            {
                try
                {
                    logger.LogInformation($"Registration attempt for email: {request.Email}");

                    // Check if user already exists
                    var existingUser = await userService.GetUserByEmailAsync(request.Email);
                    if (existingUser != null)
                    {
                        logger.LogWarning($"Registration failed - email already registered: {request.Email}");
                        return Results.BadRequest("Email is already registered");
                    }

                    // Create new user
                    var user = await userService.CreateUserAsync(
                        request.Email,
                        request.Password,
                        request.FirstName,
                        request.LastName
                    );

                    logger.LogInformation($"User registered successfully: {user.Email}");

                    return Results.Ok(new   
                           {
                               id = user.Id,
                               email = user.Email,
                               firstName = user.FirstName,
                               lastName = user.LastName,
                               role = user.Role
                           });
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error during registration");
                    return Results.InternalServerError();
                }
            });

            app.MapPost("/api/auth/logout", async Task<IResult> (   [FromServices] ILogger logger,
                                                                    [FromServices] IHttpContextAccessor httpContextAccessor) =>
            {
                try
                {
                    // Clear authentication cookies
                    httpContextAccessor.HttpContext!.Response.Cookies.Delete(JWT_COOKIE_NAME);
                    httpContextAccessor.HttpContext!.Response.Cookies.Delete(REFRESH_COOKIE_NAME);

                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error during logout");
                    return Results.InternalServerError();
                }
            });

            app.MapGet("/api/auth/profile", async Task<IResult> ([FromServices] ILogger logger,
                                                                 [FromServices] IUserService userService,
                                                                 ClaimsPrincipal claimsPrincipal) =>
            { 
                try
                {
                    //var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                    var userId = claimsPrincipal.Identity?.Name ?? string.Empty;
                    
                    if (string.IsNullOrEmpty(userId))
                    {
                        return Results.Unauthorized();
                    }

                    var user = await userService.GetUserByIdAsync(Guid.Parse(userId));
                    if (user == null)
                    {
                        return Results.NotFound();
                    }

                    return Results.Ok( new
                                       {
                                           id = user.Id,
                                           email = user.Email,
                                           firstName = user.FirstName,
                                           lastName = user.LastName,
                                           role = user.Role
                                       }
                    );
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error getting profile");
                    return Results.InternalServerError();
                }
            });

            return app;
        }
        private static void SetAuthCookies(string accessToken, string refreshToken, int expiresInSeconds, HttpContext context)
        {
            // Access token cookie
            context.Response.Cookies.Append(
                JWT_COOKIE_NAME,
                accessToken,
                GetCookieOptions(expiresInSeconds)
            );

            // Refresh token cookie (longer expiry - typically 7 days)
            context.Response.Cookies.Append(
                REFRESH_COOKIE_NAME,
                refreshToken,
                GetCookieOptions(7 * 24 * 60 * 60) // 7 days
            );
        }
        private static CookieOptions GetCookieOptions(int expiresInSeconds)
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


