using IntelligentAttendanceSystem.Interface;

namespace IntelligentAttendanceSystem
{
    public static class DeviceInitializer
    {
        public static async Task<bool> InitializeDeviceAsync(IApplicationBuilder app)
        {
            using (var scope = app.ApplicationServices.CreateScope())
            {
                var deviceService = scope.ServiceProvider.GetRequiredService<IDahuaDeviceService>();
                var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

                try
                {
                    logger.LogInformation("Attempting device auto-initialization on application startup...");

                    bool initialized = await deviceService.InitializeAndLoginAsync();

                    if (initialized)
                    {
                        logger.LogInformation("✅ Device auto-initialized successfully on startup");
                    }
                    else
                    {
                        logger.LogWarning($"❌ Device auto-initialization failed: {deviceService.InitializationError}");

                        // You can also log to a file or external service
                        if (deviceService.InitializationException != null)
                        {
                            logger.LogError(deviceService.InitializationException, "Device initialization exception details");
                        }
                    }

                    return initialized;
                }
                catch (Exception ex)
                {
                    logger.LogError(ex, "❌ Error during device auto-initialization on startup");
                    return false;
                }
            }
        }
    }
}
