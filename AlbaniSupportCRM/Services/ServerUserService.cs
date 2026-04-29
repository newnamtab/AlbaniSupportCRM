using API.Auth;

namespace API.Services
{
    public interface IServerUserService
    {
        Task<UserProfile> GetUserByIdAsync(Guid id);
        Task<UserProfile> GetUserByEmailAsync(string email);
        Task<UserProfile> CreateUserAsync(string email, string password, string firstname, string lastName);
    }

    public class ServerUserService : IServerUserService
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
