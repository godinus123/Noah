using Dapper;
using Microsoft.Data.Sqlite;

namespace Noah.Data;

public static class DbInitializer
{
    public static void InitializeSystemDb(string dbPath)
    {
        using var conn = new SqliteConnection($"Data Source={dbPath}");
        conn.Open();

        conn.Execute(@"
            CREATE TABLE IF NOT EXISTS me (
                key TEXT PRIMARY KEY,
                value TEXT
            );

            CREATE TABLE IF NOT EXISTS friends (
                user_id TEXT PRIMARY KEY,
                username TEXT NOT NULL,
                display_name TEXT,
                avatar_url TEXT,
                status_message TEXT,
                last_active INTEGER
            );

            CREATE TABLE IF NOT EXISTS saved_conversations (
                conv_id TEXT PRIMARY KEY,
                title TEXT NOT NULL,
                file_path TEXT NOT NULL,
                participant_user_ids TEXT,
                last_msg_text TEXT,
                last_msg_at INTEGER,
                msg_count INTEGER,
                file_size INTEGER,
                created_at INTEGER NOT NULL,
                last_opened_at INTEGER
            );

            CREATE TABLE IF NOT EXISTS settings (
                key TEXT PRIMARY KEY,
                value TEXT
            );
        ");
    }

    public static void InitializeConversationDb(SqliteConnection conn)
    {
        conn.Execute(@"
            CREATE TABLE IF NOT EXISTS conversation_meta (
                key TEXT PRIMARY KEY,
                value TEXT
            );

            CREATE TABLE IF NOT EXISTS messages (
                msg_id TEXT PRIMARY KEY,
                server_seq INTEGER,
                from_user_id TEXT NOT NULL,
                from_username TEXT,
                text TEXT,
                has_attachment INTEGER DEFAULT 0,
                timestamp INTEGER NOT NULL,
                is_outgoing INTEGER NOT NULL,
                is_ai INTEGER DEFAULT 0,
                status TEXT NOT NULL,
                created_at INTEGER NOT NULL
            );

            CREATE TABLE IF NOT EXISTS attachments (
                attachment_id TEXT PRIMARY KEY,
                msg_id TEXT NOT NULL,
                filename TEXT,
                mime TEXT,
                size INTEGER,
                data BLOB,
                created_at INTEGER NOT NULL
            );
        ");
    }
}
