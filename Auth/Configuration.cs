using Auth.Policy;
using Auth.Role;
using Auth.Services;
using Auth.Settings;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using Storage.Auth;
using Storage.Config;
using System.Text;

namespace Auth.Configuration
{
    public static class Configuration
    {
        public static IServiceCollection ConfigureIdentity(this IServiceCollection services, IConfiguration configuration, IHostEnvironment environment)
        {
            services.AddScoped<IJwtTokenService, JwtTokenService>();

            var jwtSettings = configuration.GetSection("JwtSettings");
            services.Configure<JwtSettings>(jwtSettings);

            // === DATABASE ===
            services.ConfigureDBContext(configuration, environment);


            // === JWT AUTHENTICATION ===
            var secretKey = Encoding.UTF8.GetBytes(jwtSettings["SecretKey"]!);
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                options.RequireHttpsMetadata = !environment.IsDevelopment();// Set true in production
                options.SaveToken = true;
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = jwtSettings["Issuer"],
                    ValidAudience = jwtSettings["Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(secretKey),
                    ClockSkew = TimeSpan.Zero
                };
                options.Events = new JwtBearerEvents
                {
                    OnAuthenticationFailed = context =>
                    {
                        if (context.Exception is SecurityTokenExpiredException)
                        {
                            context.Response.Headers.Append("Token-Expired", "true");
                        }
                        return Task.CompletedTask;
                    }
                };
            });

            // === ROLE POLICIES ===
            services.AddAuthorizationBuilder()
                            .AddPolicy(Policies.All,
                                       policy => policy.RequireRole(nameof(Roles.Admin), nameof(Roles.User)))
                            .AddPolicy(Policies.AdminOnly,
                                       policy => policy.RequireRole(nameof(Roles.Admin)))
                            .AddPolicy(Policies.UserOnly,
                                       policy => policy.RequireRole(nameof(Roles.User)));
            // OR
            //builder.Services.AddAuthorization(options =>
            //{
            //    options.AddPolicy(Policies.All, policy => 
            //                      policy.RequireRole(nameof(Roles.Admin), nameof(Roles.User)));
            //    options.AddPolicy(Policies.AdminOnly, policy =>
            //                      policy.RequireRole(nameof(Roles.Admin)));
            //    options.AddPolicy(Policies.UserOnly, policy =>
            //                      policy.RequireRole(nameof(Roles.User)));
            //});
            
            return services;
        }
        public static async Task SeedRolesAsync(IServiceProvider serviceProvider)
        {
            using var scope = serviceProvider.CreateScope();
            var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<ASMemberRole>>();
            // Seed roles
            await RoleSeeder.SeedAsync(roleManager);
        }
    }
}
