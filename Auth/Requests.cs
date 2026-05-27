using System.ComponentModel.DataAnnotations;

namespace Auth.Requests
{
    public record RegisterUser(
        [Required, EmailAddress] string Email,
        [Required, StringLength(100, MinimumLength = 2)] string FirstName,
        [Required, StringLength(100, MinimumLength = 2)] string LastName,
        [Required] string Password);
    public record LoginUser(
        [Required, EmailAddress] string Email,
        [Required] string Password);
    public record RefreshUserToken(
        [Required] Guid UserId,
        [Required] string AccessToken,
        [Required] string RefreshToken);
    public record ChangeUserPassword(
        [Required] Guid UserId,
        [Required] string CurrentPassword,
        [Required, StringLength(100, MinimumLength = 12)] string NewPassword);
}
