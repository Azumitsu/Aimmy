using Other;

namespace MouseMovementLibraries.CustomDriverSupport
{
    internal class CustomDriverMain
    {
        public static bool Load()
        {
            try
            {
                if (!CustomDriverMouse.EnsureRegistryKeyOpen())
                {
                    LogManager.Log(LogManager.LogLevel.Error, "Custom Driver: Failed to open registry key for communication.", true);
                    return false;
                }

                // Send a zero-move test packet to verify the driver is responding.
                // If the driver is loaded, the registry write will be intercepted.
                CustomDriverMouse.Move(0, 0);

                LogManager.Log(LogManager.LogLevel.Info, "Custom Driver initialized successfully.", true);
                return true;
            }
            catch (Exception ex)
            {
                LogManager.Log(LogManager.LogLevel.Error, $"Custom Driver failed to initialize: {ex.Message}", true);
                return false;
            }
        }
    }
}
