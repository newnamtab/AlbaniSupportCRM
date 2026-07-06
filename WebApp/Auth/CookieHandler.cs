using Microsoft.AspNetCore.Components.WebAssembly.Http;

namespace WebApp.Auth
{
    /// <summary>
    /// Blazor WASM's HttpClient maps to the browser fetch API, which defaults to
    /// same-origin credentials. Without this, the jwt_token/refresh_token HTTP-only
    /// cookies are never sent to (or stored from) the cross-origin API.
    /// </summary>
    public class CookieHandler : DelegatingHandler
    {
        protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            request.SetBrowserRequestCredentials(BrowserRequestCredentials.Include);
            return base.SendAsync(request, cancellationToken);
        }
    }
}
