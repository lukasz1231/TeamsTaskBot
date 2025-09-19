namespace Common.Middleware
{
    public class ApiKeyAndSafeHostMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly string _devTunnelHost;
        private readonly string _apiKey;
        private const string ApiKeyHeaderName = "x-api-key";

        public ApiKeyAndSafeHostMiddleware(RequestDelegate next, IConfiguration configuration)
        {
            _next = next;
            _devTunnelHost = configuration["Middleware:DevTunnel"]?.ToLower();
            _apiKey = configuration["Middleware:ApiKey"];
        }

        public async Task InvokeAsync(HttpContext context)
        {
            var forwardedHost = context.Request.Headers["X-Forwarded-Host"].FirstOrDefault()?.ToLower();
            var host = forwardedHost ?? context.Request.Host.Host.ToLower();
            var path = context.Request.Path.Value?.ToLower();

            // 1. trust localhost, devtunnel /api/webhook/get without API key
            bool isAllowedWithoutKey = (host == "localhost" || host == "127.0.0.1" || (host == _devTunnelHost && (path != null && (path.StartsWith("/api/webhook/get")))));

            if (isAllowedWithoutKey)
            {
                await _next(context);
                return;
            }

            // 2. If request is not trusted, check for API key
            if (!context.Request.Headers.TryGetValue(ApiKeyHeaderName, out var extractedApiKey) || !extractedApiKey.Equals(_apiKey))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                await context.Response.WriteAsync("Unauthorized: Invalid or missing API key.");
                return;
            }

            // 3. if API key is valid, proceed
            await _next(context);
        }
    }
}