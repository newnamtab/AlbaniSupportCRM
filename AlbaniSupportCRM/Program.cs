using AlbaniSupportCRM.Middleware;
using AlbaniSupportCRM.settings;
using AlbaniSupportCRM.User;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

//builder.Services.AddControllers();
//builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();

//builder.Services.AddSecurity(builder.Configuration);

builder.Services.Configure<JwtSettings>(builder.Configuration.GetSection("JwtSettings"));
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();

// Configure Identity
builder.Services.AddIdentity<ASMemberUser, IdentityRole>(options =>
{
    options.Password.RequireDigit = true; // Require at least one digit
    options.Password.RequiredLength = 8; // Minimum length of 8 characters
    options.Password.RequireNonAlphanumeric = false; // No special character required
    options.Password.RequireUppercase = true; // Require at least one UPPERCASE letter
    options.Password.RequireLowercase = true; // Require at least one lowercase letter

    options.User.RequireUniqueEmail = false;
    options.User.AllowedUserNameCharacters = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789-._@+ æøåÆØÅ"; // Allow letters, digits, and specific special characters
})
.AddEntityFrameworkStores<ASMembershipContext>()
.AddDefaultTokenProviders();

//Role Policies
builder.Services.AddAuthorizationBuilder()
                .AddPolicy(Policies.All,
                           policy => policy.RequireRole(nameof(Roles.Admin), nameof(Roles.User)))
                .AddPolicy(Policies.AdminOnly,
                           policy => policy.RequireRole(nameof(Roles.Admin)))
                .AddPolicy(Policies.UserOnly,
                           policy => policy.RequireRole(nameof(Roles.User)));

//CORS POLICY
var corsOrigins = builder.Configuration["CORS_ORIGINS"]?
    .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
    ?? Array.Empty<string>();

builder.Services.AddCors(options =>
{
    options.AddPolicy(CORSPolicies.AllowBlazorClient, policy =>
    {
        policy.WithOrigins(corsOrigins)
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

// Use DefaultConnection by default, override with env variable in Docker
var connectionString = builder.Configuration.GetConnectionString("MembershipConnection");

builder.Services.AddDbContext<ASMembershipContext>(options =>
                                                   options.UseSqlServer(connectionString)
);

//Add service on host to run background tasks, such as cleaning up expired tokens, sending notifications, etc.
//builder.Services.AddHostedService<>();

//Adding Authentication and Authorization using https://learn.microsoft.com/en-us/aspnet/core/fundamentals/minimal-apis/security?view=aspnetcore-8.0

var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

if (string.IsNullOrEmpty(secretKey))
{
    throw new Exception("Secret key not found. Ensure JwtSettings__SecretKey is set in the user secrets or configuration.");
}

var keyBytes = Convert.FromBase64String(secretKey);
var securityKey = new SymmetricSecurityKey(keyBytes);


builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = false, // We do not validate token lifetime here to allow for custom handling in OnChallenge
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = securityKey
    };
    options.Events = new JwtBearerEvents
    {
        OnChallenge = context =>
        {
            context.HandleResponse();

            context.Response.StatusCode = StatusCodes.Status401Unauthorized;
            context.Response.ContentType = "application/json";

            string message = "Unauthorized";

            if (context.AuthenticateFailure != null)
            {
                var ex = context.AuthenticateFailure;

                if (ex is SecurityTokenExpiredException)
                {
                    message = "Token expired";
                }
                else if (ex is SecurityTokenInvalidSignatureException)
                {
                    message = "Invalid token signature";
                }
                else if (ex is SecurityTokenInvalidIssuerException)
                {
                    message = "Invalid token issuer";
                }
                else if (ex is SecurityTokenInvalidAudienceException)
                {
                    message = "Invalid token audience";
                }
                else if (ex is SecurityTokenNoExpirationException)
                {
                    message = "Token has no expiration";
                }
                else if (ex is SecurityTokenInvalidLifetimeException)
                {
                    message = "Invalid token lifetime";
                }
                else
                {
                    // Fallback: use the exception message but sanitize if needed
                    message = ex.Message;
                }
            }

            return context.Response.WriteAsync($"{{\"error\": \"{message}\"}}");
        }
    };

});

builder.Services.AddAuthorization();

//builder.Services.AddInfrastructure(builder.Configuration);
var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    // app.UseSwagger();
    // app.UseSwaggerUI();
    // In development, do NOT redirect HTTP to HTTPS (to avoid CORS preflight redirect issues)
    app.MapOpenApi();
}
else
{
    // In production, enforce HTTPS
    app.UseHttpsRedirection();
}

//Middleware
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();


//Global CORS policy - can be overridden by specific endpoints if needed
app.UseCors(CORSPolicies.AllowBlazorClient);

app.UseAuthentication();
app.UseAuthorization();

//using (var scope = app.Services.CreateScope())
//{
//    var context = scope.ServiceProvider.GetRequiredService<LoyaltyContext>();
//    try
//    {
//        context.Database.Migrate(); // Automatically apply pending migrations and create tables.   
//    }
//    catch (Exception ex)
//    {
//        logger.LogError(ex, "An error occurred while applying migrations.");
//    }

//Seed Roles not already there
await DataSeeder.SeedRolesAsync(app.Services.GetRequiredService<RoleManager<IdentityRole>>());
// if(services.GetRequiredService<IHostEnvironment>().IsDevelopment())
//     await DataSeeder.SeedTestData(services, context);

app.Run();

static class DataSeeder
{
    public static async Task SeedRolesAsync(RoleManager<IdentityRole> roleManager)
    {
        // Ensure roles exist
        await EnsureRoleAsync(roleManager, nameof(Roles.Admin));
        await EnsureRoleAsync(roleManager, nameof(Roles.User));
    }

    private static async Task EnsureRoleAsync(RoleManager<IdentityRole> roleManager, string roleName)
    {
        if (!await roleManager.RoleExistsAsync(roleName))
        {
            await roleManager.CreateAsync(new IdentityRole(roleName));
        }
    }
}
