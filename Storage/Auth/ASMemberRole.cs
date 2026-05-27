using Microsoft.AspNetCore.Identity;

namespace Storage.Auth
{
    public class ASMemberRole : IdentityRole
    {
        public ASMemberRole(string roleName, string description):base(roleName)
        {
            Description = description;
            CreatedAt = DateTime.UtcNow;
        }

        public string? Description { get; private set; }
        public DateTime CreatedAt { get; private set; }
    }
}
