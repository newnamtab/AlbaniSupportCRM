using Microsoft.AspNetCore.Identity;
using Storage.Auth;

namespace Auth.Role
{
    public class Roles
    {
        public const string Admin = "admin";
        public const string User = "user";
    }

    internal static class RoleSeeder
    {
        public static async Task SeedAsync(RoleManager<ASMemberRole> roleManager)
        {
            // Ensure roles exist
            await EnsureRoleAsync(roleManager, nameof(Roles.Admin));
            await EnsureRoleAsync(roleManager, nameof(Roles.User));
        }

        private static async Task EnsureRoleAsync(RoleManager<ASMemberRole> roleManager, string roleName)
        {
            if (!await roleManager.RoleExistsAsync(roleName))
            {
                await roleManager.CreateAsync(new ASMemberRole(roleName, $"Description for: {roleName}"));
            }
        }
    }
}
