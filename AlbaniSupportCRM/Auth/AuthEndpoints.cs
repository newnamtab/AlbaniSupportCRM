using API.Auth.Requests;
using API.Auth.Responses;
using Auth.Requests;
using Auth.Services;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace API.Auth
{
    internal record Auth();
    public static class AuthEndpoints
    {
        private const string JWT_COOKIE_NAME = "jwt_token";
        private const string REFRESH_COOKIE_NAME = "refresh_token";

        public static IEndpointRouteBuilder Register(this IEndpointRouteBuilder app)
        {
            var group = app.MapGroup("/api/auth")
            .WithTags("Authentication");
            //.WithOpenApi();

            group.MapPost("/register", async ([FromBody] RegisterRequest request,
                                              [FromServices] ILogger<Auth> logger,
                                              [FromServices] IAuthenticationService _authenticationService,
                                              CancellationToken ct) =>
            {
                try
                {
                    logger.LogInformation($"Registration attempt for email: {request.Email}");

                    var newUser = await _authenticationService.RegisterUser(new RegisterUser(request.Email, request.FirstName, request.LastName, request.Password), ct);
                    if (newUser.UserId == Guid.Empty)
                    {
                        logger.LogWarning($"Registration failed - email already registered: {request.Email}");
                        return Results.BadRequest("Email is already registered");
                    }
                    logger.LogInformation($"User registered successfully: {request.Email}");

                    return Results.Ok(new RegisterResponse(newUser.UserId));
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error during registration");
                    return Results.InternalServerError();
                }
            })
            .WithName("Register")
            .ProducesValidationProblem()
            .Produces(StatusCodes.Status201Created); ;

            // Setup authentication endpoints here
            group.MapPost("/login", async ( [FromBody] LoginRequest request,
                                            [FromServices] ILogger<Auth> logger,
                                            [FromServices] IAuthenticationService _authenticationService,
                                            HttpContext httpContext,
                                            CancellationToken ct) =>
            {
                try
                {
                    if (httpContext is null) return Results.Unauthorized();

                    logger.LogInformation($"Login attempt for email: {request.Email}");

                    var loggedIn = await _authenticationService.Login(new LoginUser(request.Email, request.Password), ct);

                    if (loggedIn.UserId == Guid.Empty)
                    {
                        //"Invalid email or password" Return 401 Unauthorized for invalid credentials
                        logger.LogWarning($"Failed login attempt for email: {request.Email}");
                        return Results.Unauthorized();
                    }



                    // Set HTTP-only cookies
                    SetAuthCookies(loggedIn.AccessToken, loggedIn.AccessTokenExpiresIn,
                                   loggedIn.RefreshToken, loggedIn.RefreshTokenExpiresIn,
                                   httpContext);

                    logger.LogInformation($"Successful login for user: {loggedIn.Email}");

                    // Return user info and token expiry (token itself is in the cookie)
                    return Results.Ok(new LoginResponse(loggedIn.UserId, loggedIn.Email, loggedIn.FirstName, loggedIn.LastName));
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error during login");
                    return Results.InternalServerError();
                }
            })
            .WithName("Login");

            group.MapPost("/api/auth/refresh-token)", async ( [FromServices] ILogger<Auth> logger,
                                                              [FromServices] IAuthenticationService _authenticationService,
                                                              ClaimsPrincipal userPrincipal,
                                                              HttpContext httpContext,
                                                              CancellationToken ct) =>
            {
                try
                {
                    logger.LogInformation("Token refresh request received");

                    // Get refresh token from cookie
                    if (httpContext.Request.Cookies.TryGetValue(REFRESH_COOKIE_NAME, out var refreshToken) == false)
                    {
                        logger.LogWarning("Token not found in cookies");
                        return Results.Unauthorized();
                    }
                    //(httpContextAccessor.HttpContext!.Request.Cookies.TryGetValue(JWT_COOKIE_NAME, out var accessToken) == false);

                    var userId = userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    if (string.IsNullOrEmpty(userId)
                        || httpContext.Request.Cookies.TryGetValue(JWT_COOKIE_NAME, out var accessToken)
                        || string.IsNullOrEmpty(accessToken)) return Results.Unauthorized();

                    // Validate refresh token
                    var refreshedTokens = await _authenticationService.RefreshToken(new RefreshUserToken(Guid.Parse(userId), accessToken, refreshToken), ct);
                    if (userId == null)
                    {
                        logger.LogWarning("Invalid or expired refresh token");
                        return Results.Unauthorized();
                    }

                    // Set new access token in cookie
                    SetAuthCookies(refreshedTokens.AccessToken, refreshedTokens.AccessTokenExpiresIn,
                                   refreshedTokens.RefreshToken, refreshedTokens.RefreshTokenExpiresIn,
                                   httpContext);

                    logger.LogInformation($"Token refreshed for user: {userId}");

                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error during token refresh");
                    return Results.InternalServerError();
                }
            })
            .WithName("RefreshToken");

            group.MapPost("/change-password", async ([FromBody] ChangePasswordRequest request,
                                                     [FromServices] IAuthenticationService _authenticationService,
                                                     ClaimsPrincipal userPrincipal) =>
            {
                var userId = userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                if (string.IsNullOrEmpty(userId)) return Results.Unauthorized();

                return await _authenticationService.ChangePassword(new ChangeUserPassword(Guid.Parse(userId), request.CurrentPassword, request.NewPassword))
                ? Results.Ok()
                : Results.BadRequest();
            })
            .RequireAuthorization()
            .WithName("ChangePassword");

            group.MapPost("/logout", async ([FromServices] ILogger<Auth> logger,
                                            [FromServices] IAuthenticationService _authenticationService,
                                            ClaimsPrincipal userPrincipal,
                                            HttpContext httpContext) =>
            {
                try
                {
                    var userId = userPrincipal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                    if (string.IsNullOrEmpty(userId) == false
                        && await _authenticationService.LogOut(Guid.Parse(userId)))
                    {
                        // Clear authentication cookies
                        httpContext.Response.Cookies.Delete(JWT_COOKIE_NAME);
                        httpContext.Response.Cookies.Delete(REFRESH_COOKIE_NAME);

                        logger.LogInformation($" user: {userId} logged out");
                    }
                    return Results.Ok();
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "Error during logout");
                    return Results.InternalServerError();
                }
            })
            .RequireAuthorization()
            .WithName("Logout"); ;
            return app;
        }
        private static void SetAuthCookies(string accessToken, int accessTokenExpiresInSeconds, string refreshToken, int refreshTokenExpiresInSeconds, HttpContext context)
        {
            // Access token cookie
            context.Response.Cookies.Append(
                JWT_COOKIE_NAME,
                accessToken,
                GetCookieOptions(accessTokenExpiresInSeconds)
            );

            // Refresh token cookie (longer expiry)
            context.Response.Cookies.Append(
                REFRESH_COOKIE_NAME,
                refreshToken,
                GetCookieOptions(refreshTokenExpiresInSeconds)
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
}


