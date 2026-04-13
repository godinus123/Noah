using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.IO;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Noah.Data;
using Noah.Models;
using Serilog;

namespace Noah.ViewModels;

public partial class ChatRoomViewModel : ObservableObject
{
    private readonly Friend _friend;
    private readonly string _myUserId;

    [ObservableProperty] private string _friendDisplayName = string.Empty;
    [ObservableProperty] private string _friendUsername = string.Empty;
    [ObservableProperty] private string _inputText = string.Empty;
    [ObservableProperty] private bool _isConnected;

    public ObservableCollection<Message> Messages { get; } = new();
    public List<Attachment> Attachments { get; } = new();

    public ChatRoomViewModel(Friend friend)
    {
        _friend = friend;
        _myUserId = App.Db.GetMe("user_id") ?? "";
        FriendDisplayName = friend.DisplayName ?? friend.Username;
        FriendUsername = friend.Username;

        App.Chat.OnNewMessage += OnNewMessage;
        App.Chat.OnMessageAck += OnMessageAck;
        App.Chat.OnConnectionChanged += connected =>
            Application.Current.Dispatcher.Invoke(() => IsConnected = connected);

        IsConnected = App.Chat.IsConnected;
    }

    private void OnNewMessage(JsonElement msg)
    {
        var fromUserId = msg.TryGetProperty("from_user_id", out var f) ? f.GetString() : null;
        if (fromUserId != _friend.UserId) return;

        Application.Current.Dispatcher.Invoke(() =>
        {
            var text = msg.TryGetProperty("payload", out var p) &&
                       p.TryGetProperty("text", out var t) ? t.GetString() : null;

            var message = new Message
            {
                MsgId = msg.TryGetProperty("msg_id", out var mid) ? mid.GetString()! : Guid.NewGuid().ToString(),
                FromUserId = fromUserId!,
                FromUsername = msg.TryGetProperty("from_username", out var fn) ? fn.GetString() : _friend.Username,
                Text = text,
                Timestamp = msg.TryGetProperty("timestamp", out var ts) ? ts.GetInt64() : DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                ServerSeq = msg.TryGetProperty("server_seq", out var sq) ? sq.GetInt64() : 0,
                IsOutgoing = 0,
                IsAi = fromUserId == "ai_crew" ? 1 : 0,
                Status = "received",
                CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            };

            Messages.Add(message);
        });
    }

    private void OnMessageAck(string msgId, long serverSeq, long serverTimestamp)
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            for (int i = 0; i < Messages.Count; i++)
            {
                if (Messages[i].MsgId == msgId)
                {
                    Messages[i].ServerSeq = serverSeq;
                    Messages[i].Status = "sent";
                    break;
                }
            }
        });
    }

    [RelayCommand]
    private async Task SendMessageAsync()
    {
        var text = InputText?.Trim();
        if (string.IsNullOrEmpty(text)) return;

        var msgId = $"msg_{Guid.NewGuid():N}";
        var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        var message = new Message
        {
            MsgId = msgId,
            FromUserId = _myUserId,
            FromUsername = App.Db.GetMe("username"),
            Text = text,
            Timestamp = now,
            IsOutgoing = 1,
            IsAi = 0,
            Status = "sending",
            CreatedAt = now
        };

        Messages.Add(message);
        InputText = string.Empty;

        try
        {
            await App.Chat.SendTextAsync(msgId, _friend.UserId, text);
        }
        catch (Exception ex)
        {
            Log.Warning("Send message failed: {Error}", ex.Message);
            message.Status = "failed";
        }
    }

    [RelayCommand]
    private async Task SendFileAsync(string filePath)
    {
        if (!File.Exists(filePath)) return;

        try
        {
            var data = await File.ReadAllBytesAsync(filePath);
            var filename = Path.GetFileName(filePath);
            var mime = GetMimeType(filename);

            var uploadResult = await App.Api.UploadFileAsync(data, filename, mime);
            var fileId = uploadResult.GetProperty("file_id").GetString()!;

            var msgId = $"msg_{Guid.NewGuid():N}";
            var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            var message = new Message
            {
                MsgId = msgId,
                FromUserId = _myUserId,
                Text = $"[파일] {filename}",
                HasAttachment = 1,
                Timestamp = now,
                IsOutgoing = 1,
                Status = "sending",
                CreatedAt = now
            };

            Messages.Add(message);

            Attachments.Add(new Attachment
            {
                AttachmentId = fileId,
                MsgId = msgId,
                Filename = filename,
                Mime = mime,
                Size = data.Length,
                Data = data,
                CreatedAt = now
            });

            await App.Chat.SendFileMessageAsync(msgId, _friend.UserId, fileId, filename, mime, data.Length);
        }
        catch (Exception ex)
        {
            Log.Warning("File send failed: {Error}", ex.Message);
        }
    }

    public async Task<bool> SaveConversationAsync(string filePath, string title)
    {
        try
        {
            var convId = Guid.NewGuid().ToString();
            var messages = new List<Message>(Messages);
            await ConversationDb.SaveAsync(filePath, convId, title, messages, Attachments);

            App.Db.UpsertSavedConversation(new Conversation
            {
                ConvId = convId,
                Title = title,
                FilePath = filePath,
                ParticipantUserIds = $"{_myUserId},{_friend.UserId}",
                LastMsgText = messages.Count > 0 ? messages[^1].Text : null,
                LastMsgAt = messages.Count > 0 ? messages[^1].Timestamp : 0,
                MsgCount = messages.Count,
                FileSize = new FileInfo(filePath).Length,
                CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
                LastOpenedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
            });

            Log.Information("Conversation saved: {Path} ({Count} messages)", filePath, messages.Count);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error("Save conversation failed: {Error}", ex.Message);
            return false;
        }
    }

    public void Detach()
    {
        App.Chat.OnNewMessage -= OnNewMessage;
        App.Chat.OnMessageAck -= OnMessageAck;
    }

    private static string GetMimeType(string filename)
    {
        var ext = Path.GetExtension(filename).ToLowerInvariant();
        return ext switch
        {
            ".png" => "image/png",
            ".jpg" or ".jpeg" => "image/jpeg",
            ".gif" => "image/gif",
            ".pdf" => "application/pdf",
            ".txt" => "text/plain",
            ".zip" => "application/zip",
            _ => "application/octet-stream"
        };
    }
}
