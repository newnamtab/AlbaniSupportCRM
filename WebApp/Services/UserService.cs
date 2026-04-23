using System.Net.Http.Json;
using WebApp.Models;

namespace WebApp.Services
{
    public interface IUserService
    {
        Task Register(AddUser model);
        Task<IList<User>> GetAll();
        Task<User> GetById(string id);
        Task Update(string id, EditUser model);
        Task Delete(string id);
    }

    public class UserService
    {
        private readonly HttpClient _httpClient;

        public UserService(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task Register(AddUser model)
        {
            await _httpClient.PostAsJsonAsync("/users/register", model);
        }

        public async Task<IList<User>> GetAll()
        {
            var users = await _httpClient.GetFromJsonAsync<IList<User>>("/users");
            return users ?? new List<User>();
        }

        public async Task<User> GetById(string id)
        {
            var user = await _httpClient.GetFromJsonAsync<User>($"/users/{id}");

            return user ?? User.Empty;
        }

        public async Task Update(string id, EditUser model)
        {
            await _httpClient.PutAsJsonAsync($"/users/{id}", model);
        }

        public async Task Delete(string id)
        {
            await _httpClient.DeleteAsync($"/users/{id}");
        }
    }
}
