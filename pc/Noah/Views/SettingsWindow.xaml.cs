using System.Windows;

namespace Noah.Views;

public partial class SettingsWindow : Window
{
    public SettingsWindow()
    {
        InitializeComponent();

        TxtServerUrl.Text = App.Db.GetSetting("server_url") ?? AppInfo.DefaultServerUrl;
        TxtSaveFolder.Text = App.Db.GetSetting("default_save_folder") ?? AppInfo.DefaultSaveFolder;
    }

    private void BtnBrowse_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new System.Windows.Forms.FolderBrowserDialog
        {
            Description = "기본 저장 폴더 선택",
            SelectedPath = TxtSaveFolder.Text
        };
        if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
        {
            TxtSaveFolder.Text = dlg.SelectedPath;
        }
    }

    private void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        App.Db.SetSetting("server_url", TxtServerUrl.Text.Trim());
        App.Db.SetSetting("default_save_folder", TxtSaveFolder.Text.Trim());
        App.Api.ServerUrl = TxtServerUrl.Text.Trim();
        MessageBox.Show("설정이 저장되었습니다.", "NOAH", MessageBoxButton.OK, MessageBoxImage.Information);
        Close();
    }
}
