using Microsoft.AspNetCore.Identity;
using Storage.Auth;

namespace MembershipProfile
{
    //public interface IMembershipProfileService
    //{
    //    Task<UserProfile> CreateUserAsync(string email, string password, string firstName, string lastName);
    //    Task<UserProfile> GetUserByIdAsync(Guid id);
    //    Task<UserProfile> GetUserByEmailAsync(string email);
    //}
    //
    //internal class MemberShipProfileService : IMembershipProfileService
    //{
    //    //private readonly IMembershipContext asMembershipContext;
    //    private readonly UserManager<ASMemberUser> _userManager;
    //    public async Task<UserProfile> CreateUserAsync(string email, string password, string firstName, string lastName)
    //    {
    //        var existingUser =_userManager.FindByEmailAsync(email);
    //       
    //        if (existingUser != null)
    //        {
    //            // Handle the case where the user already exists, e.g., throw an exception or return a specific result
    //            return UserProfile.Empty();
    //        }
    //        var newMember = new ASMemberUser
    //        {
    //            UserName = email,
    //            Email = email,
    //            FirstName = firstName,
    //            LastName = lastName,
    //            Id = Guid.NewGuid().ToString()
    //        };
    //        var newMemberResult = await _userManager.CreateAsync(newMember, HashPassword(password));
    //          
    //        // Optionally, assign a default role to the new user
    //        var addToRoleResult = await _userManager.AddToRoleAsync(newMember, Roles.User);
    //
    //        return newMemberResult.Succeeded && addToRoleResult.Succeeded
    //             ? UserProfile.New(Guid.Parse(newMember.Id), newMember.Email, newMember.FirstName, newMember.LastName, Roles.User, DateTime.UtcNow )
    //             : UserProfile.Empty();
    //    }
    //    public async Task<UserProfile> GetUserByIdAsync(Guid id)
    //    {
    //        return UserProfile.Empty();
    //    }
    //    public async Task<UserProfile> GetUserByEmailAsync(string email)
    //    {
    //        return UserProfile.Empty();
    //    }
    //    private string HashPassword(string password)
    //    {
    //        // Implement a secure password hashing mechanism here (e.g., using BCrypt or PBKDF2)
    //        return Convert.ToBase64String(System.Text.Encoding.UTF8.GetBytes(password)); // Simple hash for demo
    //    }
    //}
}
