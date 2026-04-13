using System.Windows;
using Noah.Data;
using Noah.Services;
using Serilog;

namespace Noah;

public partial class App : Application
{
    public static SystemDb Db { get; private set; } = null!;
    public static ApiClient Api { get; private set; } = null!;
    public static WebSocketClient Ws { get; private set; } = null!;
    public static ChatService Chat { get; set; } = null!;

    private void OnStartup(object sender, StartupEventArgs e)
    {
        LogService.Initialize();
        Log.Information("Data path: {DataPath}", AppInfo.DataPath);

        Db = new SystemDb(AppInfo.DbPath);

        var serverUrl = Db.GetSetting("server_url") ?? AppInfo.DefaultServerUrl;
        Api = new ApiClient(serverUrl);
        Ws = new WebSocketClient();

        // Check for saved token → auto-login
        var token = Db.GetMe("token");
        if (!string.IsNullOrEmpty(token))
        {
            Api.Token = token;
            var deviceId = Db.GetMe("device_id") ?? AppInfo.DeviceId;
            Chat = new ChatService(Api, Ws, deviceId);

            var main = new MainWindow();
            main.Show();

            // Connect WS in background
            var wsUrl = serverUrl.Replace("https://", "wss://").Replace("http://", "ws://") + "/ws";
            _ = Ws.ConnectAsync(wsUrl, token, deviceId);
        }
        else
        {
            Chat = new ChatService(Api, Ws, AppInfo.DeviceId);
            var login = new Views.LoginPage();
            login.Show();
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _ = Ws.DisconnectAsync();
        Log.Information("NOAH exiting");
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
