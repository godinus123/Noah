using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Noah.Models;
using Noah.ViewModels;
using Noah.Views;

namespace Noah;

public partial class MainWindow : Window
{
    private readonly MainViewModel _vm = new();

    public MainWindow()
    {
        InitializeComponent();

        TxtMyName.Text = $"{_vm.MyDisplayName} (@{_vm.MyUsername})";
        FriendList.ItemsSource = _vm.Friends;

        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(_vm.IsConnected))
            {
                StatusDot.Fill = _vm.IsConnected
                    ? new SolidColorBrush(Color.FromRgb(34, 197, 94))
                    : new SolidColorBrush(Color.FromRgb(239, 68, 68));
                TxtStatus.Text = _vm.IsConnected ? "연결됨" : "연결 끊김";
            }
            if (e.PropertyName == nameof(_vm.StatusMessage))
                TxtStatus.Text = _vm.StatusMessage;
        };

        _vm.OnOpenChat += OpenChatWindow;

        Loaded += async (_, _) => await _vm.LoadFriendsCommand.ExecuteAsync(null);
    }

    private void OpenChatWindow(Friend friend)
    {
        var chatWindow = new ChatRoomWindow(friend);
        chatWindow.Show();
    }

    private async void BtnAddFriend_Click(object sender, RoutedEventArgs e)
    {
        _vm.AddFriendUsername = TxtAddFriend.Text;
        await _vm.AddFriendCommand.ExecuteAsync(null);
        TxtAddFriend.Text = string.Empty;
    }

    private void TxtAddFriend_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
            BtnAddFriend_Click(sender, e);
    }

    private void FriendList_DoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (FriendList.SelectedItem is Friend friend)
            _vm.OpenChatCommand.Execute(friend);
    }

    private void BtnSettings_Click(object sender, RoutedEventArgs e)
    {
        var settings = new SettingsWindow();
        settings.Owner = this;
        settings.ShowDialog();
    }

    private void BtnLogout_Click(object sender, RoutedEventArgs e)
    {
        var result = MessageBox.Show("로그아웃 하시겠습니까?", "NOAH", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (result == MessageBoxResult.Yes)
            _vm.LogoutCommand.Execute(null);
    }
}
