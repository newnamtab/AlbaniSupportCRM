namespace AlbaniSupportCRM.Middleware
{
    public class RequestLoggingMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<RequestLoggingMiddleware> _logger;

        public RequestLoggingMiddleware(RequestDelegate next, ILogger<RequestLoggingMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context)
        {
            // Log the request details
            _logger.LogInformation("Incoming Request: {Method} {Path}", context.Request.Method, context.Request.Path);

            // Log the Authorization header
            if (context.Request.Headers.ContainsKey("Authorization"))
            {
                var authHeader = context.Request.Headers["Authorization"].ToString();
                _logger.LogInformation("Authorization Header: {AuthHeader}", authHeader);
            }

            // Log user claims if authenticated
            if (context.User.Identity?.IsAuthenticated ?? false)
            {
                var userClaims = context.User.Claims.Select(c => new { c.Type, c.Value }).ToList();
                _logger.LogInformation("User Claims: {UserClaims}", userClaims);
            }
            else
            {
                _logger.LogInformation("User Is Not Authenticated");
            }

            // Call the next middleware in the pipeline
            await _next(context);
        }
    }
}
