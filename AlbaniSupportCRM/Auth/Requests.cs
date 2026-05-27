using System.ComponentModel.DataAnnotations;

namespace API.Auth.Requests
{
    public record RegisterRequest(
        [Required, EmailAddress] string Email,
        [Required, StringLength(100, MinimumLength = 2)] string FirstName,
        [Required, StringLength(100, MinimumLength = 2)] string LastName,
        [Required] string Password);
    public record LoginRequest(
        [Required, EmailAddress] string Email,
        [Required] string Password);
    public record RefreshTokenRequest(
        [Required] string AccessToken,
        [Required] string RefreshToken);
    public record ChangePasswordRequest(
        [Required] string CurrentPassword,
        [Required, StringLength(100, MinimumLength = 12)] string NewPassword);
}
