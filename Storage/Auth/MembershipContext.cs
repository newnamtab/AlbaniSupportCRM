using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace Storage.Auth
{
    internal class MembershipContext(DbContextOptions<MembershipContext> options)
                   : IdentityDbContext<ASMemberUser, ASMemberRole, string>(options)
    {
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);
            modelBuilder.Entity<ASMemberUser>(entity =>
            {
                entity.HasIndex(e => e.Email)
                      .IsUnique();
                
                entity.HasIndex(e => e.IsActive);
                
                entity.Property(e => e.FirstName)
                      .HasMaxLength(100);
                
                entity.Property(e => e.LastName)
                      .HasMaxLength(100);
            });
            modelBuilder.Entity<IdentityUserLogin<string>>(entity =>
            {
                entity.HasKey(e => new { e.LoginProvider, e.ProviderKey });
            });
            modelBuilder.Entity<IdentityUserRole<string>>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.RoleId });
            });
            modelBuilder.Entity<IdentityUserToken<string>>(entity =>
            {
                entity.HasKey(e => new { e.UserId, e.LoginProvider, e.Name });
            });
        }
    }
}
