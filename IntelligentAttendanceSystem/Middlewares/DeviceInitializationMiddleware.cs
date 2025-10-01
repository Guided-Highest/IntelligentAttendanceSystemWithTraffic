using IntelligentAttendanceSystem.Interface;

namespace IntelligentAttendanceSystem.Middlewares
{
    public class DeviceInitializationMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<DeviceInitializationMiddleware> _logger;

        public DeviceInitializationMiddleware(RequestDelegate next, ILogger<DeviceInitializationMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task InvokeAsync(HttpContext context, IDahuaDeviceService deviceService)
        {
            // Skip for specific paths that don't need device initialization check
            var path = context.Request.Path.Value?.ToLower() ?? "";

            if (path.StartsWith("/device/") ||
                path.StartsWith("/error") ||
                path.StartsWith("/home") ||
                path == "/")
            {
                await _next(context);
                return;
            }

            // If device is not initialized and we're trying to access protected pages
            if (!deviceService.IsInitialized && !deviceService.IsDeviceConnected)
            {
                _logger.LogWarning($"Device not initialized. Redirecting from {path} to LoginFailed page.");

                // Store the original request path for potential return after login
                context.Items["OriginalPath"] = path;

                // Redirect to LoginFailed page
                context.Response.Redirect("/Device/LoginFailed");
                return;
            }

            await _next(context);
        }
    }
}
