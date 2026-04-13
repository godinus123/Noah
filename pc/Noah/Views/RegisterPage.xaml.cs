using System.Windows;
using Noah.ViewModels;

namespace Noah.Views;

public partial class RegisterPage : Window
{
    private readonly RegisterViewModel _vm = new();

    public RegisterPage()
    {
        InitializeComponent();

        _vm.OnRegisterSuccess += () =>
        {
            var main = new MainWindow();
            main.Show();
            Close();
        };

        _vm.OnNavigateToLogin += () =>
        {
            var login = new LoginPage();
            login.Show();
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

    private async void BtnRegister_Click(object sender, RoutedEventArgs e)
    {
        _vm.Username = TxtUsername.Text;
        _vm.DisplayName = TxtDisplayName.Text;
        _vm.Password = TxtPassword.Password;
        await _vm.RegisterCommand.ExecuteAsync(null);
    }

    private void LinkLogin_Click(object sender, RoutedEventArgs e)
    {
        _vm.GoToLoginCommand.Execute(null);
    }
}
