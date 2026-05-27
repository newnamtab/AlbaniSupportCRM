using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Storage.Auth;

namespace Storage.Config
{
    public static class StorageConfigurationExtensions
    {
        // Extension method to wrap configuration of internal MembershipContext with SQL Server and retry logic
        public static IServiceCollection ConfigureDBContext(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
        {
            // === DATABASE ===
            services.AddDbContext<MembershipContext>(options =>
                options.UseSqlServer(
                    configuration.GetConnectionString("DefaultConnection"),
                    sqlOptions =>
                    {
                        sqlOptions.EnableRetryOnFailure(3);
                    }));

            // === IDENTITY ===
            services.AddIdentity<ASMemberUser, ASMemberRole>(options =>
            {
                // Password policy (OWASP compliant)
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 12;
                options.Password.RequiredUniqueChars = 6;
                // Lockout
                options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(15);
                options.Lockout.MaxFailedAccessAttempts = 5;
                options.Lockout.AllowedForNewUsers = true;
                // User settings
                options.User.RequireUniqueEmail = true;
                options.User.AllowedUserNameCharacters =
                    "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+";
                // Sign-in
                options.SignIn.RequireConfirmedEmail = !environment.IsDevelopment(); // Set true in production
                options.SignIn.RequireConfirmedPhoneNumber = false;
            })
            .AddEntityFrameworkStores<MembershipContext>()
            .AddDefaultTokenProviders()
            .AddTokenProvider<DataProtectorTokenProvider<ASMemberUser>>("Refresh");

            return services;
        }
    }
}
