using API.Middleware;
using API.Settings;
using API.Auth;
using Auth.Configuration;

var builder = WebApplication.CreateBuilder(args);

// Configure logging
// Add logging
//builder.Services.AddLogging();

// Add services to the container.
// Learn more about configuring OpenAPI at https://aka.ms/aspnet/openapi
builder.Services.AddOpenApi();

//builder.Services.AddControllers();
//builder.Services.AddEndpointsApiExplorer();
builder.Services.AddHttpContextAccessor();

builder.Services.ConfigureIdentity( builder.Configuration, builder.Environment );


// === CORS POLICIES ===
builder.Services.AddCors(options =>
{
    options.AddPolicy(CORSPolicies.AllowBlazorClient, policy =>
    {
        policy
            .WithOrigins("http://localhost:5227") // Your Blazor WASM URL
            .AllowAnyMethod()
            .AllowAnyHeader()
            .AllowCredentials(); // Important for cookies
    });
});

// === APPLICATION BACKGROUND SERVICES ===
//Add service on host to run background tasks, such as cleaning up expired tokens, sending notifications, etc.
//builder.Services.AddHostedService<>();

// === SWAGGER WITH AUTH ===
//builder.Services.AddEndpointsApiExplorer();
//builder.Services.AddSwaggerGen(options =>
//{
//    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
//    {
//        Name = "Authorization",
//        Description = "Enter 'Bearer {token}'",
//        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
//        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey,
//        Scheme = "Bearer"
//    });
//    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
//    {
//        {
//            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
//            {
//                Reference = new Microsoft.OpenApi.Models.OpenApiReference
//                {
//                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
//                    Id = "Bearer"
//                }
//            },
//            Array.Empty<string>()
//        }
//    });
//});
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
    app.UseHsts();
    app.UseHttpsRedirection();
}

//Middleware
app.UseMiddleware<RequestLoggingMiddleware>();
app.UseMiddleware<ExceptionHandlingMiddleware>();


// Map endpoints
AuthEndpoints.Register(app);

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
app.Run();