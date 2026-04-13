using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Noah.Models;
using Serilog;

namespace Noah.ViewModels;

public partial class MainViewModel : ObservableObject
{
    [ObservableProperty] private string _myDisplayName = string.Empty;
    [ObservableProperty] private string _myUsername = string.Empty;
    [ObservableProperty] private bool _isConnected;
    [ObservableProperty] private string _addFriendUsername = string.Empty;
    [ObservableProperty] private string _statusMessage = string.Empty;

    public ObservableCollection<Friend> Friends { get; } = new();

    public event Action<Friend>? OnOpenChat;

    public MainViewModel()
    {
        MyDisplayName = App.Db.GetMe("display_name") ?? "NOAH";
        MyUsername = App.Db.GetMe("username") ?? "";

        App.Chat.OnConnectionChanged += connected =>
        {
            Application.Current.Dispatcher.Invoke(() => IsConnected = connected);
        };

        App.Chat.OnNewMessage += msg =>
        {
            Application.Current.Dispatcher.Invoke(() => HandleIncomingMessage(msg));
        };
    }

    [RelayCommand]
    private async Task LoadFriendsAsync()
    {
        try
        {
            var result = await App.Api.GetFriendsAsync();
            var friends = result.EnumerateArray().Select(f => new Friend
            {
                UserId = f.GetProperty("user_id").GetString()!,
                Username = f.GetProperty("username").GetString()!,
                DisplayName = f.TryGetProperty("display_name", out var dn) ? dn.GetString() : null,
                AvatarUrl = f.TryGetProperty("avatar_url", out var av) ? av.GetString() : null,
                StatusMessage = f.TryGetProperty("status_message", out var sm) ? sm.GetString() : null,
            }).ToList();

            Friends.Clear();
            foreach (var friend in friends)
            {
                Friends.Add(friend);
                App.Db.UpsertFriend(friend);
            }
        }
        catch (Exception ex)
        {
            Log.Warning("Load friends failed, using local cache: {Error}", ex.Message);
            var cached = App.Db.GetFriends();
            Friends.Clear();
            foreach (var f in cached) Friends.Add(f);
        }
    }

    [RelayCommand]
    private async Task AddFriendAsync()
    {
        if (string.IsNullOrWhiteSpace(AddFriendUsername))
        {
            StatusMessage = "사용자 이름을 입력하세요.";
            return;
        }

        try
        {
            var result = await App.Api.AddFriendAsync(AddFriendUsername.Trim());
            var friend = new Friend
            {
                UserId = result.GetProperty("user_id").GetString()!,
                Username = result.GetProperty("username").GetString()!,
                DisplayName = result.TryGetProperty("display_name", out var dn) ? dn.GetString() : null,
            };

            if (!Friends.Any(f => f.UserId == friend.UserId))
            {
                Friends.Add(friend);
                App.Db.UpsertFriend(friend);
            }

            AddFriendUsername = string.Empty;
            StatusMessage = $"{friend.Username} 추가 완료!";
            Log.Information("Friend added: {Username}", friend.Username);
        }
        catch (Exception ex)
        {
            Log.Warning("Add friend failed: {Error}", ex.Message);
            StatusMessage = ex.Message.Contains("404") ? "사용자를 찾을 수 없습니다." :
                            ex.Message.Contains("400") ? "자기 자신은 추가할 수 없습니다." :
                            $"친구 추가 실패: {ex.Message}";
        }
    }

    [RelayCommand]
    private void OpenChat(Friend? friend)
    {
        if (friend != null) OnOpenChat?.Invoke(friend);
    }

    private void HandleIncomingMessage(JsonElement msg)
    {
        // Refresh friends list to show latest message preview
        _ = LoadFriendsAsync();
    }

    [RelayCommand]
    private void Logout()
    {
        App.Db.SetMe("token", "");
        _ = App.Ws.DisconnectAsync();
        var login = new Views.LoginPage();
        login.Show();

        foreach (Window w in Application.Current.Windows)
        {
            if (w != login) w.Close();
        }
    }
}
