using System;
using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Input;
using System.Windows.Media;
using Microsoft.Win32;
using Noah.Models;
using Noah.ViewModels;

namespace Noah.Views;

public partial class ChatRoomWindow : Window
{
    private readonly ChatRoomViewModel _vm;

    public ChatRoomWindow(Friend friend)
    {
        InitializeComponent();

        _vm = new ChatRoomViewModel(friend);
        TxtFriendName.Text = _vm.FriendDisplayName;
        TxtFriendUsername.Text = $"@{_vm.FriendUsername}";
        TxtAvatar.Text = _vm.FriendDisplayName[..1].ToUpper();
        Title = $"NOAH - {_vm.FriendDisplayName}";

        MessageList.ItemsSource = _vm.Messages;

        _vm.Messages.CollectionChanged += (_, _) =>
        {
            MessageScroll.ScrollToEnd();
        };

        _vm.PropertyChanged += (_, e) =>
        {
            if (e.PropertyName == nameof(_vm.IsConnected))
            {
                StatusDot.Fill = _vm.IsConnected
                    ? new SolidColorBrush(Color.FromRgb(34, 197, 94))
                    : new SolidColorBrush(Color.FromRgb(239, 68, 68));
            }
        };

        StatusDot.Fill = _vm.IsConnected
            ? new SolidColorBrush(Color.FromRgb(34, 197, 94))
            : new SolidColorBrush(Color.FromRgb(239, 68, 68));
    }

    private async void BtnSend_Click(object sender, RoutedEventArgs e)
    {
        _vm.InputText = TxtInput.Text;
        await _vm.SendMessageCommand.ExecuteAsync(null);
        TxtInput.Text = string.Empty;
        TxtInput.Focus();
    }

    private void TxtInput_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter && !Keyboard.Modifiers.HasFlag(ModifierKeys.Shift))
        {
            e.Handled = true;
            BtnSend_Click(sender, e);
        }

        // Ctrl+V for image paste
        if (e.Key == Key.V && Keyboard.Modifiers.HasFlag(ModifierKeys.Control))
        {
            if (Clipboard.ContainsImage())
            {
                e.Handled = true;
                PasteImage();
            }
        }
    }

    private async void PasteImage()
    {
        var image = Clipboard.GetImage();
        if (image == null) return;

        var encoder = new System.Windows.Media.Imaging.PngBitmapEncoder();
        encoder.Frames.Add(System.Windows.Media.Imaging.BitmapFrame.Create(image));

        var tempPath = Path.Combine(Path.GetTempPath(), $"noah_paste_{DateTime.Now:yyyyMMdd_HHmmss}.png");
        using (var fs = new FileStream(tempPath, FileMode.Create))
        {
            encoder.Save(fs);
        }

        await _vm.SendFileCommand.ExecuteAsync(tempPath);
    }

    private async void BtnAttach_Click(object sender, RoutedEventArgs e)
    {
        var dlg = new OpenFileDialog
        {
            Title = "파일 첨부",
            Filter = "모든 파일 (*.*)|*.*|이미지 (*.png;*.jpg;*.gif)|*.png;*.jpg;*.gif|PDF (*.pdf)|*.pdf"
        };

        if (dlg.ShowDialog() == true)
        {
            await _vm.SendFileCommand.ExecuteAsync(dlg.FileName);
        }
    }

    private void OnDragOver(object sender, DragEventArgs e)
    {
        e.Effects = e.Data.GetDataPresent(DataFormats.FileDrop)
            ? DragDropEffects.Copy : DragDropEffects.None;
        e.Handled = true;
    }

    private async void OnDrop(object sender, DragEventArgs e)
    {
        if (e.Data.GetDataPresent(DataFormats.FileDrop))
        {
            var files = (string[])e.Data.GetData(DataFormats.FileDrop)!;
            foreach (var file in files)
            {
                await _vm.SendFileCommand.ExecuteAsync(file);
            }
        }
    }

    private async void OnClosing(object? sender, CancelEventArgs e)
    {
        if (_vm.Messages.Count == 0)
        {
            _vm.Detach();
            return;
        }

        var result = MessageBox.Show(
            $"대화 내용을 저장하시겠습니까?\n({_vm.Messages.Count}개 메시지)",
            "NOAH - 대화 저장",
            MessageBoxButton.YesNoCancel,
            MessageBoxImage.Question);

        if (result == MessageBoxResult.Cancel)
        {
            e.Cancel = true;
            return;
        }

        if (result == MessageBoxResult.Yes)
        {
            var dlg = new SaveFileDialog
            {
                Title = "대화 저장",
                Filter = "NOAH 대화 파일 (*.noahdb)|*.noahdb",
                FileName = $"chat_{DateTime.Now:yyyy-MM-dd}_{_vm.FriendUsername}.noahdb",
                InitialDirectory = AppInfo.DefaultSaveFolder
            };

            if (dlg.ShowDialog() == true)
            {
                var title = $"{_vm.FriendDisplayName}과의 대화 ({DateTime.Now:yyyy-MM-dd})";
                var saved = await _vm.SaveConversationAsync(dlg.FileName, title);
                if (!saved)
                {
                    MessageBox.Show("저장 실패!", "NOAH", MessageBoxButton.OK, MessageBoxImage.Error);
                    e.Cancel = true;
                    return;
                }
            }
            else
            {
                e.Cancel = true;
                return;
            }
        }

        _vm.Detach();
    }
}
