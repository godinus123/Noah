-- NOAH v0.1 서버 DB 스키마
-- SQLite (better-sqlite3)

PRAGMA journal_mode = WAL;
PRAGMA foreign_keys = ON;

-- 사용자
CREATE TABLE IF NOT EXISTS users (
    user_id TEXT PRIMARY KEY,
    username TEXT UNIQUE NOT NULL,
    password_hash TEXT NOT NULL,
    display_name TEXT,
    avatar_url TEXT,
    status_message TEXT,
    created_at INTEGER NOT NULL,
    last_seen INTEGER
);
CREATE INDEX IF NOT EXISTS idx_users_username ON users(username);

-- 디바이스
CREATE TABLE IF NOT EXISTS devices (
    device_id TEXT PRIMARY KEY,
    user_id TEXT NOT NULL,
    device_name TEXT,
    device_type TEXT,
    last_seen INTEGER,
    is_online INTEGER DEFAULT 0,
    created_at INTEGER NOT NULL,
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE
);
CREATE INDEX IF NOT EXISTS idx_devices_user ON devices(user_id);

-- 친구 관계 (양방향)
CREATE TABLE IF NOT EXISTS friendships (
    user_id TEXT NOT NULL,
    friend_user_id TEXT NOT NULL,
    created_at INTEGER NOT NULL,
    PRIMARY KEY (user_id, friend_user_id),
    FOREIGN KEY (user_id) REFERENCES users(user_id) ON DELETE CASCADE,
    FOREIGN KEY (friend_user_id) REFERENCES users(user_id) ON DELETE CASCADE
);

-- 메시지 큐 (배달 대기, 7일 만료)
CREATE TABLE IF NOT EXISTS pending_messages (
    msg_id TEXT NOT NULL,
    target_device_id TEXT NOT NULL,
    from_user_id TEXT NOT NULL,
    target_user_id TEXT NOT NULL,
    type TEXT NOT NULL,
    payload TEXT NOT NULL,
    server_seq INTEGER NOT NULL,
    created_at INTEGER NOT NULL,
    expires_at INTEGER NOT NULL,
    PRIMARY KEY (msg_id, target_device_id)
);
CREATE INDEX IF NOT EXISTS idx_pending_target ON pending_messages(target_device_id);
CREATE INDEX IF NOT EXISTS idx_pending_expires ON pending_messages(expires_at);

-- 첨부 파일 임시 저장
CREATE TABLE IF NOT EXISTS pending_files (
    file_id TEXT PRIMARY KEY,
    msg_id TEXT,
    from_user_id TEXT NOT NULL,
    filename TEXT,
    mime TEXT,
    size INTEGER,
    storage_path TEXT NOT NULL,
    created_at INTEGER NOT NULL,
    expires_at INTEGER NOT NULL
);

-- 글로벌 sequence
CREATE TABLE IF NOT EXISTS seq_counter (
    name TEXT PRIMARY KEY,
    value INTEGER NOT NULL DEFAULT 0
);
INSERT OR IGNORE INTO seq_counter (name, value) VALUES ('msg_seq', 0);

-- AI 봇 사용자 (시스템 계정)
INSERT OR IGNORE INTO users (
    user_id, username, password_hash, display_name, status_message, created_at
) VALUES (
    'ai_crew',
    '크루',
    'NO_PASSWORD',
    '🕊️ 크루',
    'NOAH AI Assistant (Claude)',
    strftime('%s', 'now') * 1000
);
