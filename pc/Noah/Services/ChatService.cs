using System;
using System.Collections.Concurrent;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog;

namespace Noah.Services;

public class ChatService
{
    private readonly ApiClient _api;
    private readonly WebSocketClient _ws;
    private readonly string _deviceId;
    private readonly ConcurrentQueue<PendingMessage> _outQueue = new();

    public event Action<JsonElement>? OnNewMessage;
    public event Action<string, long, long>? OnMessageAck; // msgId, serverSeq, serverTimestamp
    public event Action<bool>? OnConnectionChanged;

    public bool IsConnected => _ws.IsConnected;

    public ChatService(ApiClient api, WebSocketClient ws, string deviceId)
    {
        _api = api;
        _ws = ws;
        _deviceId = deviceId;

        _ws.OnMessage += HandleMessage;
        _ws.OnConnected += () =>
        {
            OnConnectionChanged?.Invoke(true);
            _ = FlushQueueAsync();
        };
        _ws.OnDisconnected += () => OnConnectionChanged?.Invoke(false);
    }

    private void HandleMessage(JsonElement msg)
    {
        var type = msg.TryGetProperty("type", out var t) ? t.GetString() : null;

        switch (type)
        {
            case "new_message":
                OnNewMessage?.Invoke(msg);
                // Auto-ACK
                if (msg.TryGetProperty("msg_id", out var mid))
                {
                    _ = _ws.SendAsync(new { type = "ack", msg_ids = new[] { mid.GetString() } });
                }
                break;

            case "message_ack":
                if (msg.TryGetProperty("msg_id", out var ackId) &&
                    msg.TryGetProperty("server_seq", out var seq) &&
                    msg.TryGetProperty("server_timestamp", out var ts))
                {
                    OnMessageAck?.Invoke(ackId.GetString()!, seq.GetInt64(), ts.GetInt64());
                }
                break;

            case "auth_ok":
                Log.Information("WebSocket authenticated as {UserId}",
                    msg.TryGetProperty("user_id", out var uid) ? uid.GetString() : "?");
                break;

            case "auth_error":
                Log.Error("WebSocket auth failed: {Error}",
                    msg.TryGetProperty("error", out var err) ? err.GetString() : "unknown");
                break;

            case "pong":
                break;

            case "error":
                Log.Warning("WS error: {Error}",
                    msg.TryGetProperty("error", out var e) ? e.GetString() : "unknown");
                break;
        }
    }

    public async Task SendTextAsync(string msgId, string targetUserId, string text)
    {
        var msg = new
        {
            type = "message",
            msg_id = msgId,
            target_user_id = targetUserId,
            msg_type = "text",
            payload = new { text }
        };

        if (_ws.IsConnected)
        {
            await _ws.SendAsync(msg);
        }
        else
        {
            _outQueue.Enqueue(new PendingMessage(msgId, targetUserId, "text", new { text }));
            Log.Debug("Message queued (offline): {MsgId}", msgId);
        }
    }

    public async Task SendFileMessageAsync(string msgId, string targetUserId, string fileId, string filename, string mime, long size)
    {
        var payload = new { file_id = fileId, filename, mime, size };
        var msg = new
        {
            type = "message",
            msg_id = msgId,
            target_user_id = targetUserId,
            msg_type = "file",
            payload
        };

        if (_ws.IsConnected)
        {
            await _ws.SendAsync(msg);
        }
        else
        {
            _outQueue.Enqueue(new PendingMessage(msgId, targetUserId, "file", payload));
        }
    }

    private async Task FlushQueueAsync()
    {
        while (_outQueue.TryDequeue(out var pending))
        {
            try
            {
                if (_ws.IsConnected)
                {
                    await _ws.SendAsync(new
                    {
                        type = "message",
                        msg_id = pending.MsgId,
                        target_user_id = pending.TargetUserId,
                        msg_type = pending.MsgType,
                        payload = pending.Payload
                    });
                    Log.Debug("Queued message sent: {MsgId}", pending.MsgId);
                }
                else
                {
                    _outQueue.Enqueue(pending);
                    break;
                }
            }
            catch (Exception ex)
            {
                Log.Warning("Failed to send queued message: {Error}", ex.Message);
                _outQueue.Enqueue(pending);
                break;
            }
        }
    }

    private record PendingMessage(string MsgId, string TargetUserId, string MsgType, object Payload);
}
