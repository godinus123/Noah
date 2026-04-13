using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace Noah.ViewModels;

public partial class RegisterViewModel : ObservableObject
{
    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _displayName = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isLoading;

    public event Action? OnRegisterSuccess;
    public event Action? OnNavigateToLogin;

    [RelayCommand]
    private async Task RegisterAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "사용자 이름과 비밀번호를 입력하세요.";
            return;
        }
        if (Username.Length < 2 || Username.Length > 30)
        {
            ErrorMessage = "사용자 이름은 2~30자여야 합니다.";
            return;
        }
        if (Password.Length < 4)
        {
            ErrorMessage = "비밀번호는 4자 이상이어야 합니다.";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var name = string.IsNullOrWhiteSpace(DisplayName) ? Username : DisplayName;
            var result = await App.Api.RegisterAsync(Username, Password, name);

            var userId = result.GetProperty("user_id").GetString()!;
            var token = result.GetProperty("token").GetString()!;

            App.Db.SetMe("user_id", userId);
            App.Db.SetMe("username", Username);
            App.Db.SetMe("display_name", name);
            App.Db.SetMe("token", token);

            App.Api.Token = token;
            var devResult = await App.Api.RegisterDeviceAsync(Environment.MachineName, "windows");
            var deviceId = devResult.GetProperty("device_id").GetString()!;
            App.Db.SetMe("device_id", deviceId);

            App.Chat = new Services.ChatService(App.Api, App.Ws, deviceId);

            var serverUrl = App.Db.GetSetting("server_url") ?? AppInfo.DefaultServerUrl;
            var wsUrl = serverUrl.Replace("https://", "wss://").Replace("http://", "ws://") + "/ws";
            _ = App.Ws.ConnectAsync(wsUrl, token, deviceId);

            Log.Information("Register success: {Username} ({UserId})", Username, userId);
            OnRegisterSuccess?.Invoke();
        }
        catch (Exception ex)
        {
            Log.Warning("Register failed: {Error}", ex.Message);
            ErrorMessage = ex.Message.Contains("409") ? "이미 사용 중인 사용자 이름입니다." :
                           $"가입 실패: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void GoToLogin()
    {
        OnNavigateToLogin?.Invoke();
    }
}
