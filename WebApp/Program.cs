using Blazor.Extensions.Logging;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using WebApp;
using WebApp.Auth;
using WebApp.Services;

var builder = WebAssemblyHostBuilder.CreateDefault(args);
builder.RootComponents.Add<App>("#app");
builder.RootComponents.Add<HeadOutlet>("head::after");

// Configure HTTP client with base address
builder.Services.AddHttpClient("API", (client) =>
{
    client.BaseAddress = new Uri(builder.Configuration["apiUrl"]);
});

builder.Services.AddScoped<IAccountService, AccountService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
builder.Services.AddScoped<IClientUserService, ClientUserService>();
builder.Services.AddScoped<IAlertService, AlertService>();

// Add authentication
builder.Services.AddAuthorizationCore();
//builder.Services.AddApiAuthorization();

// Add logging
builder.Services.AddLogging(logging =>
{
    logging.AddBrowserConsole();
});

await builder.Build().RunAsync();