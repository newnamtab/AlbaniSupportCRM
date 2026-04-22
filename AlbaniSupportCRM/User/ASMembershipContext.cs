using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace AlbaniSupportCRM.User
{
    public class ASMembershipContext : IdentityDbContext<ASMemberUser>
    {
        public ASMembershipContext(DbContextOptions<ASMembershipContext> options) : base(options)
        {
        }

        public DbSet<ASMemberUser> ASMemberUsers { get; set; } = null!;

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ASMemberUser>()
               .HasIndex(y => y.Email)
               .IsUnique();

            modelBuilder.Entity<ASMemberUser>()
                .HasIndex(y => y.PhoneNumber)
                .IsUnique();

            //modelBuilder.Entity<ASMemberUser>()
            //    .HasOne(a => a.Yearcard)
            //    .WithOne(y => y.User)
            //    .HasForeignKey<Yearcard>(y => y.UserId)
            //    .OnDelete(DeleteBehavior.Cascade)
            //    .IsRequired();

            base.OnModelCreating(modelBuilder);
        }
    }
}
