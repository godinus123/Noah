using System.Collections.Generic;
using System.Linq;
using Dapper;
using Microsoft.Data.Sqlite;
using Noah.Models;

namespace Noah.Data;

public class SystemDb
{
    private readonly string _connectionString;

    public SystemDb(string dbPath)
    {
        _connectionString = $"Data Source={dbPath}";
        DbInitializer.InitializeSystemDb(dbPath);
    }

    private SqliteConnection Open()
    {
        var conn = new SqliteConnection(_connectionString);
        conn.Open();
        return conn;
    }

    // ── me table ──

    public string? GetMe(string key)
    {
        using var conn = Open();
        return conn.QueryFirstOrDefault<string>(
            "SELECT value FROM me WHERE key = @key", new { key });
    }

    public void SetMe(string key, string value)
    {
        using var conn = Open();
        conn.Execute(
            "INSERT OR REPLACE INTO me (key, value) VALUES (@key, @value)",
            new { key, value });
    }

    // ── settings table ──

    public string? GetSetting(string key)
    {
        using var conn = Open();
        return conn.QueryFirstOrDefault<string>(
            "SELECT value FROM settings WHERE key = @key", new { key });
    }

    public void SetSetting(string key, string value)
    {
        using var conn = Open();
        conn.Execute(
            "INSERT OR REPLACE INTO settings (key, value) VALUES (@key, @value)",
            new { key, value });
    }

    // ── friends table ──

    public List<Friend> GetFriends()
    {
        using var conn = Open();
        return conn.Query<Friend>(
            "SELECT user_id AS UserId, username AS Username, display_name AS DisplayName, " +
            "avatar_url AS AvatarUrl, status_message AS StatusMessage, last_active AS LastActive " +
            "FROM friends ORDER BY username").ToList();
    }

    public void UpsertFriend(Friend friend)
    {
        using var conn = Open();
        conn.Execute(@"
            INSERT OR REPLACE INTO friends (user_id, username, display_name, avatar_url, status_message, last_active)
            VALUES (@UserId, @Username, @DisplayName, @AvatarUrl, @StatusMessage, @LastActive)",
            friend);
    }

    public void RemoveFriend(string userId)
    {
        using var conn = Open();
        conn.Execute("DELETE FROM friends WHERE user_id = @userId", new { userId });
    }

    // ── saved_conversations table ──

    public List<Conversation> GetSavedConversations()
    {
        using var conn = Open();
        return conn.Query<Conversation>(
            "SELECT conv_id AS ConvId, title AS Title, file_path AS FilePath, " +
            "participant_user_ids AS ParticipantUserIds, last_msg_text AS LastMsgText, " +
            "last_msg_at AS LastMsgAt, msg_count AS MsgCount, file_size AS FileSize, " +
            "created_at AS CreatedAt, last_opened_at AS LastOpenedAt " +
            "FROM saved_conversations ORDER BY last_msg_at DESC").ToList();
    }

    public void UpsertSavedConversation(Conversation conv)
    {
        using var conn = Open();
        conn.Execute(@"
            INSERT OR REPLACE INTO saved_conversations
            (conv_id, title, file_path, participant_user_ids, last_msg_text, last_msg_at, msg_count, file_size, created_at, last_opened_at)
            VALUES (@ConvId, @Title, @FilePath, @ParticipantUserIds, @LastMsgText, @LastMsgAt, @MsgCount, @FileSize, @CreatedAt, @LastOpenedAt)",
            conv);
    }

    public void RemoveSavedConversation(string convId)
    {
        using var conn = Open();
        conn.Execute("DELETE FROM saved_conversations WHERE conv_id = @convId", new { convId });
    }
}
