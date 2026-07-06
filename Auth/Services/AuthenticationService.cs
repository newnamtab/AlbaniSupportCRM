using Auth.Requests;
using Auth.Results;
using Microsoft.AspNetCore.Identity;
using Storage.Auth;

namespace Auth.Services
{
    public interface IAuthenticationService
    {
        Task<RegisterResult> RegisterUser(RegisterUser request, CancellationToken ct);
        Task<LoginResult> Login(LoginUser request, CancellationToken ct);
        Task<RefreshTokenResult> RefreshToken(RefreshUserToken request, CancellationToken ct);
        Task<bool> ChangePassword(ChangeUserPassword request);
        Task<bool> LogOut(Guid userId);
    }

    internal class AuthenticationService : IAuthenticationService
    {
        private readonly UserManager<ASMemberUser> _userManager;
        private readonly SignInManager<ASMemberUser> _signInManager;
        private readonly IJwtTokenService _tokenService;

        public AuthenticationService(UserManager<ASMemberUser> userManager,
                                     SignInManager<ASMemberUser> signInManager,
                                     IJwtTokenService tokenService)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenService = tokenService;
        }

        public async Task<RegisterResult> RegisterUser(RegisterUser request, CancellationToken ct)
        {
            var existingUser = await _userManager.FindByEmailAsync(request.Email);
            if (existingUser != null)
                return new RegisterResult(Guid.Empty); //Results.Conflict(new { Error = "Email already registered" });
            var user = new ASMemberUser
            {
                UserName = request.Email,
                Email = request.Email,
                FirstName = request.FirstName,
                LastName = request.LastName,
                CreatedAt = DateTime.UtcNow
            };
            var result = await _userManager.CreateAsync(user, request.Password);
            if (!result.Succeeded)
            {
                return new RegisterResult(Guid.Empty);
            }
            await _userManager.AddToRoleAsync(user, "User");
            return new RegisterResult(Guid.Parse(user.Id));
        }
        public async Task<LoginResult> Login(LoginUser request, CancellationToken ct)
        {
            var user = await _userManager.FindByEmailAsync(request.Email);

            if (user == null || !user.IsActive)
                return new LoginResult(Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty, 0, string.Empty, 0);

            var result = await _signInManager.CheckPasswordSignInAsync(
                user, request.Password, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                if (result.IsLockedOut)
                    //return Results.Problem(
                    //    statusCode: StatusCodes.Status423Locked,
                    //    title: "Account locked",
                    //    detail: "Too many failed attempts. Try again later.");
                    return new LoginResult(Guid.Empty, string.Empty, string.Empty, string.Empty, string.Empty, 0, string.Empty, 0);
            }
            user.LastLoginAt = DateTime.UtcNow;
            await _userManager.UpdateAsync(user);
            var tokens = await _tokenService.GenerateTokensAsync(user);

            return new LoginResult(Guid.Parse(user.Id),
                    user.Email ?? "unknown@email.com",
                    user.FirstName,
                    user.LastName,
                    tokens.AccessToken,
                    tokens.AccessTokenExpiresIn,
                    tokens.RefreshToken,
                    tokens.RefreshTokenExpiresIn);
        }
        public async Task<RefreshTokenResult> RefreshToken(RefreshUserToken request, CancellationToken ct)
        {
            var user = await _userManager.FindByIdAsync( request.UserId.ToString() );
            if (user == null ||
                user.RefreshToken != request.RefreshToken ||
                user.RefreshTokenExpiry <= DateTime.UtcNow)
            {
                return new RefreshTokenResult(string.Empty, 0, string.Empty, 0);// Results.Unauthorized();
            }
            var tokens = await _tokenService.GenerateTokensAsync(user);
            return new RefreshTokenResult(tokens.AccessToken, tokens.AccessTokenExpiresIn,
                                          tokens.RefreshToken, tokens.RefreshTokenExpiresIn);// Results.Ok(tokens);
        }
        public async Task<bool> ChangePassword(ChangeUserPassword request)
        {
            var user = await _userManager.FindByIdAsync(request.UserId.ToString());

            if (user == null) return false;
            var result = await _userManager.ChangePasswordAsync(user,
                                                                request.CurrentPassword,
                                                                request.NewPassword);
            return result.Succeeded;
        }
        public async Task<bool> LogOut(Guid userId)
        {
            var user = await _userManager.FindByIdAsync(userId.ToString());

            if (user != null)
            {
                user.RefreshToken = null;
                user.RefreshTokenExpiry = null;
                await _userManager.UpdateAsync(user);
            }
            return true;
        }
    }
}
