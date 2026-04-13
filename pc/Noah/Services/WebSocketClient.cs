using System;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Serilog;

namespace Noah.Services;

public class WebSocketClient
{
    private ClientWebSocket? _ws;
    private int _retryDelay = 1000;
    private const int MaxDelay = 30000;
    private CancellationTokenSource? _cts;

    public event Action<JsonElement>? OnMessage;
    public event Action? OnConnected;
    public event Action? OnDisconnected;

    public bool IsConnected => _ws?.State == WebSocketState.Open;

    public async Task ConnectAsync(string url, string token, string deviceId)
    {
        _cts?.Cancel();
        _cts = new CancellationTokenSource();
        _ = Task.Run(() => ConnectWithRetryAsync(url, token, deviceId, _cts.Token));
        await Task.CompletedTask;
    }

    private async Task ConnectWithRetryAsync(string url, string token, string deviceId, CancellationToken ct)
    {
        while (!ct.IsCancellationRequested)
        {
            try
            {
                _ws?.Dispose();
                _ws = new ClientWebSocket();
                _ws.Options.SetRequestHeader("ngrok-skip-browser-warning", "true");

                Log.Information("WebSocket connecting to {Url}", url);
                await _ws.ConnectAsync(new Uri(url), ct);

                // Send auth
                var authMsg = JsonSerializer.Serialize(new
                {
                    type = "auth",
                    token,
                    device_id = deviceId
                });
                await SendRawAsync(authMsg, ct);

                _retryDelay = 1000;
                OnConnected?.Invoke();
                Log.Information("WebSocket connected");

                await ReceiveLoopAsync(ct);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Log.Warning("WebSocket connection failed: {Message}, retry in {Delay}ms", ex.Message, _retryDelay);
                OnDisconnected?.Invoke();

                try { await Task.Delay(_retryDelay, ct); }
                catch (OperationCanceledException) { break; }

                _retryDelay = Math.Min(_retryDelay * 2, MaxDelay);
            }
        }
    }

    private async Task ReceiveLoopAsync(CancellationToken ct)
    {
        var buffer = new byte[8192];
        var sb = new StringBuilder();

        while (_ws?.State == WebSocketState.Open && !ct.IsCancellationRequested)
        {
            sb.Clear();
            WebSocketReceiveResult result;

            do
            {
                result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), ct);

                if (result.MessageType == WebSocketMessageType.Close)
                {
                    Log.Information("WebSocket closed by server");
                    OnDisconnected?.Invoke();
                    return;
                }

                sb.Append(Encoding.UTF8.GetString(buffer, 0, result.Count));
            } while (!result.EndOfMessage);

            var text = sb.ToString();
            if (string.IsNullOrEmpty(text)) continue;

            try
            {
                var json = JsonSerializer.Deserialize<JsonElement>(text);
                Log.Debug("WS recv: {Type}", json.TryGetProperty("type", out var t) ? t.GetString() : "unknown");
                OnMessage?.Invoke(json);
            }
            catch (JsonException ex)
            {
                Log.Warning("Invalid WS message: {Error}", ex.Message);
            }
        }

        OnDisconnected?.Invoke();
    }

    public async Task SendAsync(object message)
    {
        if (_ws?.State != WebSocketState.Open) return;
        var json = JsonSerializer.Serialize(message);
        await SendRawAsync(json, _cts?.Token ?? CancellationToken.None);
    }

    private async Task SendRawAsync(string text, CancellationToken ct)
    {
        if (_ws?.State != WebSocketState.Open) return;
        var bytes = Encoding.UTF8.GetBytes(text);
        await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, ct);
    }

    public async Task SendPingAsync()
    {
        await SendAsync(new { type = "ping" });
    }

    public async Task DisconnectAsync()
    {
        _cts?.Cancel();
        if (_ws?.State == WebSocketState.Open)
        {
            try
            {
                await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
            }
            catch { /* ignore */ }
        }
        _ws?.Dispose();
        _ws = null;
    }
}
