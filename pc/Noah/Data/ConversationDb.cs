using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Dapper;
using Microsoft.Data.Sqlite;
using Noah.Models;

namespace Noah.Data;

public static class ConversationDb
{
    public static async Task SaveAsync(string filePath, string convId, string title,
        List<Message> messages, List<Attachment> attachments)
    {
        if (File.Exists(filePath)) File.Delete(filePath);

        var dir = Path.GetDirectoryName(filePath);
        if (dir != null) Directory.CreateDirectory(dir);

        using var conn = new SqliteConnection($"Data Source={filePath}");
        await conn.OpenAsync();

        DbInitializer.InitializeConversationDb(conn);

        await conn.ExecuteAsync(@"
            INSERT INTO conversation_meta (key, value) VALUES
            ('conv_id', @ConvId),
            ('title', @Title),
            ('created_at', @CreatedAt),
            ('noah_version', '0.1')",
            new { ConvId = convId, Title = title, CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() });

        if (messages.Count > 0)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO messages (msg_id, server_seq, from_user_id, from_username, text, has_attachment, timestamp, is_outgoing, is_ai, status, created_at)
                VALUES (@MsgId, @ServerSeq, @FromUserId, @FromUsername, @Text, @HasAttachment, @Timestamp, @IsOutgoing, @IsAi, @Status, @CreatedAt)",
                messages);
        }

        if (attachments.Count > 0)
        {
            await conn.ExecuteAsync(@"
                INSERT INTO attachments (attachment_id, msg_id, filename, mime, size, data, created_at)
                VALUES (@AttachmentId, @MsgId, @Filename, @Mime, @Size, @Data, @CreatedAt)",
                attachments);
        }
    }

    public static async Task<(List<Message> Messages, List<Attachment> Attachments)> LoadAsync(string filePath)
    {
        using var conn = new SqliteConnection($"Data Source={filePath};Mode=ReadOnly");
        await conn.OpenAsync();

        var messages = (await conn.QueryAsync<Message>(
            "SELECT msg_id AS MsgId, server_seq AS ServerSeq, from_user_id AS FromUserId, " +
            "from_username AS FromUsername, text AS Text, has_attachment AS HasAttachment, " +
            "timestamp AS Timestamp, is_outgoing AS IsOutgoing, is_ai AS IsAi, " +
            "status AS Status, created_at AS CreatedAt " +
            "FROM messages ORDER BY server_seq, timestamp")).ToList();

        var attachments = (await conn.QueryAsync<Attachment>(
            "SELECT attachment_id AS AttachmentId, msg_id AS MsgId, filename AS Filename, " +
            "mime AS Mime, size AS Size, data AS Data, created_at AS CreatedAt " +
            "FROM attachments")).ToList();

        return (messages, attachments);
    }
}
