using API.Auth;

namespace API.Services
{
    public interface IUserService
    {
        Task<UserProfile> GetUserByIdAsync(Guid id);
        Task<UserProfile> GetUserByEmailAsync(string email);
        Task<UserProfile> CreateUserAsync(string email, string password, string firstname, string lastName);
    }

    public class UserService
    {
        public async Task<UserProfile> GetUserByIdAsync(Guid id)
        {
            return new UserProfile();
        }
        public async Task<UserProfile> GetUserByEmailAsync(string email)
        {
            return new UserProfile();
        }
        public async Task<UserProfile> CreateUserAsync(string email, string password, string firstname, string lastName)
        {                                               
            return new UserProfile();                   
        }                                               
    }
}
