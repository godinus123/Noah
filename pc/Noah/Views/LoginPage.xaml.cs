using System.Windows;
using Noah.ViewModels;

namespace Noah.Views;

public partial class LoginPage : Window
{
    private readonly LoginViewModel _vm = new();

    public LoginPage()
    {
        InitializeComponent();

        _vm.OnLoginSuccess += () =>
        {
            var main = new MainWindow();
            main.Show();
            Close();
        };

        _vm.OnNavigateToRegister += () =>
        {
            var reg = new RegisterPage();
            reg.Show();
            Close();
        };

        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(_vm.ErrorMessage))
                TxtError.Text = _vm.ErrorMessage;
            if (e.PropertyName == nameof(_vm.IsLoading))
                LoadingOverlay.Visibility = _vm.IsLoading ? Visibility.Visible : Visibility.Collapsed;
        };
    }

    private async void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        _vm.Username = TxtUsername.Text;
        _vm.Password = TxtPassword.Password;
        await _vm.LoginCommand.ExecuteAsync(null);
    }

    private void LinkRegister_Click(object sender, RoutedEventArgs e)
    {
        _vm.GoToRegisterCommand.Execute(null);
    }

    private void BtnSkipLogin_Click(object sender, RoutedEventArgs e)
    {
        // Guest mode - skip auth, go straight to main
        var name = string.IsNullOrWhiteSpace(TxtUsername.Text) ? "비손피씨" : TxtUsername.Text.Trim();
        App.Db.SetMe("user_id", "guest_local");
        App.Db.SetMe("username", name);
        App.Db.SetMe("display_name", name);
        App.Db.SetMe("device_id", AppInfo.DeviceId);

        // Default friends
        var defaults = new[]
        {
            new Noah.Models.Friend { UserId = "ai_crew", Username = "크루", DisplayName = "크루 (AI)" },
            new Noah.Models.Friend { UserId = "agent_bison_server", Username = "비손서버", DisplayName = "비손서버" },
            new Noah.Models.Friend { UserId = "agent_bison_pc", Username = "비손피씨", DisplayName = "비손피씨" },
            new Noah.Models.Friend { UserId = "agent_anmok", Username = "안목", DisplayName = "안목" },
            new Noah.Models.Friend { UserId = "user_hyoseung", Username = "효승", DisplayName = "효승" },
        };
        foreach (var f in defaults)
            App.Db.UpsertFriend(f);

        App.Chat = new Services.ChatService(App.Api, App.Ws, AppInfo.DeviceId);

        var main = new MainWindow();
        main.Show();
        Close();
    }
}
