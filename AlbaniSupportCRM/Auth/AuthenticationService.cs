namespace API.Auth
{
    public interface IAuthenticationService
    {
        Task<UserProfile> AuthenticateAsync(string email, string password);
        Task<bool> StoreRefreshTokenAsync(Guid userId, string refreshToken);
    }
    public class AuthenticationService
    {
        public async Task<UserProfile> AuthenticateAsync(string email, string password)
        {
            return new UserProfile
            {
                Id = Guid.Empty,
                Email = email,
                FirstName = "John",
                LastName = "Doe",
                Role = "Admin",
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };
        }
        public async Task<bool> StoreRefreshTokenAsync(Guid userId,  string refreshToken)
        {
            return true;
        }
    }
}
