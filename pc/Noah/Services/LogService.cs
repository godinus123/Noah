using System.IO;
using Serilog;

namespace Noah.Services;

public static class LogService
{
    public static void Initialize()
    {
        Directory.CreateDirectory(AppInfo.LogPath);

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.File(
                Path.Combine(AppInfo.LogPath, "noah-.log"),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                outputTemplate: "{Timestamp:yyyy-MM-dd HH:mm:ss.fff} [{Level:u3}] {Message:lj}{NewLine}{Exception}")
            .CreateLogger();

        Log.Information("NOAH v{Version} started. DeviceId={DeviceId}", AppInfo.AppVersion, AppInfo.DeviceId);
    }
}
