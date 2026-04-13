using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Serilog;

namespace Noah.Services;

public class ApiClient
{
    private readonly HttpClient _http;
    private string _serverUrl;
    private string? _token;

    public string ServerUrl
    {
        get => _serverUrl;
        set => _serverUrl = value.TrimEnd('/');
    }

    public string? Token
    {
        get => _token;
        set
        {
            _token = value;
            _http.DefaultRequestHeaders.Authorization =
                value != null ? new AuthenticationHeaderValue("Bearer", value) : null;
        }
    }

    public ApiClient(string serverUrl)
    {
        _serverUrl = serverUrl.TrimEnd('/');
        _http = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
        _http.DefaultRequestHeaders.Add("ngrok-skip-browser-warning", "true");
    }

    private async Task<JsonElement> PostAsync(string path, object body)
    {
        var json = JsonSerializer.Serialize(body);
        var content = new StringContent(json, Encoding.UTF8, "application/json");
        var resp = await _http.PostAsync($"{_serverUrl}{path}", content);
        var text = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            Log.Warning("API POST {Path} failed: {Status} {Body}", path, resp.StatusCode, text);
            throw new ApiException((int)resp.StatusCode, text);
        }

        return JsonSerializer.Deserialize<JsonElement>(text);
    }

    private async Task<JsonElement> GetAsync(string path)
    {
        var resp = await _http.GetAsync($"{_serverUrl}{path}");
        var text = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            Log.Warning("API GET {Path} failed: {Status} {Body}", path, resp.StatusCode, text);
            throw new ApiException((int)resp.StatusCode, text);
        }

        return JsonSerializer.Deserialize<JsonElement>(text);
    }

    private async Task<JsonElement> DeleteAsync(string path)
    {
        var resp = await _http.DeleteAsync($"{_serverUrl}{path}");
        var text = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
        {
            Log.Warning("API DELETE {Path} failed: {Status} {Body}", path, resp.StatusCode, text);
            throw new ApiException((int)resp.StatusCode, text);
        }

        return JsonSerializer.Deserialize<JsonElement>(text);
    }

    // ── Auth ──

    public async Task<JsonElement> RegisterAsync(string username, string password, string? displayName = null)
    {
        return await PostAsync("/api/auth/register", new
        {
            username,
            password,
            display_name = displayName ?? username
        });
    }

    public async Task<JsonElement> LoginAsync(string username, string password)
    {
        return await PostAsync("/api/auth/login", new { username, password });
    }

    // ── Me ──

    public async Task<JsonElement> GetMeAsync()
    {
        return await GetAsync("/api/me");
    }

    public async Task UpdateMeAsync(string? displayName = null, string? statusMessage = null)
    {
        var body = new System.Collections.Generic.Dictionary<string, string>();
        if (displayName != null) body["display_name"] = displayName;
        if (statusMessage != null) body["status_message"] = statusMessage;
        await PostAsync("/api/me", body);
    }

    // ── Friends ──

    public async Task<JsonElement> AddFriendAsync(string username)
    {
        return await PostAsync("/api/friends/add", new { username });
    }

    public async Task<JsonElement> GetFriendsAsync()
    {
        return await GetAsync("/api/friends");
    }

    public async Task RemoveFriendAsync(string userId)
    {
        await DeleteAsync($"/api/friends/{userId}");
    }

    // ── Devices ──

    public async Task<JsonElement> RegisterDeviceAsync(string deviceName, string deviceType = "windows")
    {
        return await PostAsync("/api/devices/register", new
        {
            device_name = deviceName,
            device_type = deviceType
        });
    }

    // ── Messages (HTTP fallback) ──

    public async Task<JsonElement> SendMessageAsync(string msgId, string targetUserId, string type, object payload)
    {
        return await PostAsync("/api/messages", new
        {
            msg_id = msgId,
            target_user_id = targetUserId,
            type,
            payload
        });
    }

    public async Task<JsonElement> GetPendingMessagesAsync(string deviceId)
    {
        var req = new HttpRequestMessage(HttpMethod.Get, $"{_serverUrl}/api/messages/pending");
        req.Headers.Add("device-id", deviceId);
        var resp = await _http.SendAsync(req);
        var text = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
            throw new ApiException((int)resp.StatusCode, text);

        return JsonSerializer.Deserialize<JsonElement>(text);
    }

    public async Task AckMessagesAsync(string deviceId, string[] msgIds)
    {
        var req = new HttpRequestMessage(HttpMethod.Post, $"{_serverUrl}/api/messages/ack");
        req.Headers.Add("device-id", deviceId);
        req.Content = new StringContent(
            JsonSerializer.Serialize(new { msg_ids = msgIds }),
            Encoding.UTF8, "application/json");
        await _http.SendAsync(req);
    }

    // ── Files ──

    public async Task<JsonElement> UploadFileAsync(byte[] data, string filename, string mime)
    {
        var content = new MultipartFormDataContent();
        var fileContent = new ByteArrayContent(data);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(mime);
        content.Add(fileContent, "file", filename);
        var resp = await _http.PostAsync($"{_serverUrl}/api/files/upload", content);
        var text = await resp.Content.ReadAsStringAsync();

        if (!resp.IsSuccessStatusCode)
            throw new ApiException((int)resp.StatusCode, text);

        return JsonSerializer.Deserialize<JsonElement>(text);
    }

    public async Task<byte[]> DownloadFileAsync(string fileId)
    {
        var resp = await _http.GetAsync($"{_serverUrl}/api/files/{fileId}");
        if (!resp.IsSuccessStatusCode)
            throw new ApiException((int)resp.StatusCode, "File download failed");
        return await resp.Content.ReadAsByteArrayAsync();
    }
}

public class ApiException : Exception
{
    public int StatusCode { get; }
    public string ResponseBody { get; }

    public ApiException(int statusCode, string responseBody)
        : base($"API error {statusCode}: {responseBody}")
    {
        StatusCode = statusCode;
        ResponseBody = responseBody;
    }
}
