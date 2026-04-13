using System;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Serilog;

namespace Noah.ViewModels;

public partial class LoginViewModel : ObservableObject
{
    [ObservableProperty] private string _username = string.Empty;
    [ObservableProperty] private string _password = string.Empty;
    [ObservableProperty] private string _errorMessage = string.Empty;
    [ObservableProperty] private bool _isLoading;

    public event Action? OnLoginSuccess;
    public event Action? OnNavigateToRegister;

    [RelayCommand]
    private async Task LoginAsync()
    {
        if (string.IsNullOrWhiteSpace(Username) || string.IsNullOrWhiteSpace(Password))
        {
            ErrorMessage = "사용자 이름과 비밀번호를 입력하세요.";
            return;
        }

        IsLoading = true;
        ErrorMessage = string.Empty;

        try
        {
            var result = await App.Api.LoginAsync(Username, Password);

            var userId = result.GetProperty("user_id").GetString()!;
            var token = result.GetProperty("token").GetString()!;
            var username = result.GetProperty("username").GetString()!;
            var displayName = result.GetProperty("display_name").GetString() ?? username;

            App.Db.SetMe("user_id", userId);
            App.Db.SetMe("username", username);
            App.Db.SetMe("display_name", displayName);
            App.Db.SetMe("token", token);

            // Register device
            App.Api.Token = token;
            var devResult = await App.Api.RegisterDeviceAsync(Environment.MachineName, "windows");
            var deviceId = devResult.GetProperty("device_id").GetString()!;
            App.Db.SetMe("device_id", deviceId);

            // Init chat service with real device_id
            App.Chat = new Services.ChatService(App.Api, App.Ws, deviceId);

            // Connect WebSocket
            var serverUrl = App.Db.GetSetting("server_url") ?? AppInfo.DefaultServerUrl;
            var wsUrl = serverUrl.Replace("https://", "wss://").Replace("http://", "ws://") + "/ws";
            _ = App.Ws.ConnectAsync(wsUrl, token, deviceId);

            Log.Information("Login success: {Username} ({UserId})", username, userId);
            OnLoginSuccess?.Invoke();
        }
        catch (Exception ex)
        {
            Log.Warning("Login failed: {Error}", ex.Message);
            ErrorMessage = ex.Message.Contains("401") ? "사용자 이름 또는 비밀번호가 잘못되었습니다." :
                           ex.Message.Contains("timeout") ? "서버 연결 시간 초과" :
                           $"로그인 실패: {ex.Message}";
        }
        finally
        {
            IsLoading = false;
        }
    }

    [RelayCommand]
    private void GoToRegister()
    {
        OnNavigateToRegister?.Invoke();
    }
}
