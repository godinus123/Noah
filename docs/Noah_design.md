# Noah — 종합 설계 문서

**프로젝트명**: Noah (노아)
**작성자**: agent_crew
**작성일**: 2026-04-13
**상태**: Phase 1 설계 확정, 구현 대기 (NeoStock Phase 7 이후 시작)
**목표**: 디바이스 기반 P2P 메신저 + AI 에이전트 통합

---

## 0. 브랜딩

### 0.1 이름의 의미

**Noah (노아)** — 창세기 8장 8-12절에서 노아가 비둘기를 보내 물이 빠졌는지 확인한 데서 따옴.

```
"... 비둘기를 자기에게서 내놓아 ...
저녁때에 비둘기가 그에게로 돌아왔는데 
그 입에 감람나무 새 잎사귀가 있는지라
이에 노아가 땅에 물이 줄어든 줄을 알았으며"
                                — 창세기 8:11
```

**비둘기 = 메시지 전달자**, **노아 = 메시지를 보내고 받는 사람**.
디지털 시대의 노아의 비둘기 = Noah 메신저.

### 0.2 시각 정체성

- **아이콘**: 편지를 입에 문 비둘기 (사용자 제공)
- **주 색상**: 평화로운 하늘색 / 흰색 / 노란 편지
- **태그라인**: "방주에서 보낸 메시지" 또는 "비둘기처럼 빠르게"

### 0.3 AI 채팅방 컨셉

```
🛶 방주 [The Ark]              ← AI 에이전트들이 모이는 메인 채팅방
├── 🕊️ 크루 (지휘)
├── 🐦 안목 (모바일 빌드)
├── 🦅 비손피씨 (Windows 빌드)
├── 🦉 비손서버 (Linux 서버)
└── 👤 이효승 (사용자)
```

각 AI는 새의 종류로 비유 (비둘기/매/부엉이 등). 모두 노아의 방주에 모인 새들.

### 0.4 네이밍 규칙

```
프로젝트명: Noah
폴더명: Noah
GitHub: godinus123/Noah
EXE: Noah_{DeviceName}.exe (Noah_BisonPC.exe, Noah_Anmok.exe, Noah_Crew.exe)
패키지(Android): com.noah.anmok
서버 디렉토리: /home/godinus/Noah/
첨부 저장: /var/Noah/files/
DB: noah_server.db
```

---

## 1. 프로젝트 개요

### 1.1 목적
- 카카오톡과 유사한 사용자 경험의 메신저 구현
- 기존 git push 기반 AI 에이전트 통신을 실시간 메신저로 통합
- 디바이스 간 P2P + 서버 큐 fallback 아키텍처
- 서버는 데이터를 영구 저장하지 않음 (배달 후 삭제)

### 1.2 핵심 가치
- **빠른 통신**: WebSocket/WebRTC로 80ms 응답
- **개인정보 보호**: 서버에 메시지 영구 저장 안 함
- **AI 통합**: 사람과 AI 에이전트가 같은 채팅방에 공존
- **확장성**: 10만 사용자까지 단일 서버 처리

### 1.3 핵심 의사결정 요약

| 항목 | 결정 |
|------|------|
| 통신 방식 | WebRTC P2P + WebSocket 폴백 |
| 서버 데이터 | 디바이스/큐만, 메시지 본문 임시 저장 (배달 후 삭제) |
| 클라이언트 DB | messages.db (텍스트) + attachments.db (첨부 메타) 분리 |
| 시간 정렬 | server_seq 발급 (전역 순번) |
| 사용자 개념 | Phase 1에서 단순화 (모두 "이효승") |
| 디바이스 식별 | 빌드 시 AssemblyName으로 분리, EXE 다름 |
| 푸시 알림 | FCM (Android), Toast (Windows) |
| 배지 | 가능한 단말만 (Samsung 등) |
| 키보드 | OS 기본 한국어 키보드 그대로 |
| AI 통합 | Phase 1B (Phase 1A 후) |
| UI 라이브러리 | Syncfusion SfChat (모바일) + HandyControl (PC) |

---

## 2. 아키텍처

### 2.1 시스템 구성도

```
┌─────────────────────────────────────────────────────────┐
│              Noah 시스템 구성                          │
└─────────────────────────────────────────────────────────┘

┌───────────────┐       ┌──────────────────┐       ┌──────────────┐
│  Mobile MAUI  │       │  Linux Server    │       │  PC WPF      │
│  (Android)    │       │  (비손서버)       │       │  (Windows)   │
│               │       │                  │       │              │
│  - 안목       │       │  Node.js + ws    │       │  - 비손피씨  │
│  - (확장가능) │       │  + SQLite        │       │  - 안목      │
│               │       │                  │       │  - (확장가능)│
└───────┬───────┘       └────────┬─────────┘       └──────┬───────┘
        │                        │                        │
        │  ┌─────WebRTC P2P──────┼──────────────────────┐ │
        │  │  (DataChannel)      │                      │ │
        │  │                     │                      │ │
        │  └─────────────────────┼──────────────────────┘ │
        │                        │                        │
        └──── WebSocket ─────────┼──── WebSocket ─────────┘
              (시그널링 + 폴백)    │      (시그널링 + 폴백)
                                 │
                                 ↓
                          ┌──────────────┐
                          │  FCM (Google) │
                          │  푸시 알림    │
                          └──────┬───────┘
                                 │
                          ┌──────┴───────┐
                          │  Mobile only │
                          └──────────────┘

추가 노드 (AI 에이전트, Phase 1B):

┌──────────────┐    ┌──────────────┐    ┌──────────────┐
│  Linux       │    │  Web Browser │    │  Linux       │
│  비손서버 AI  │    │  크루 AI     │    │  Android     │
│  (Node)      │    │  (Node 또는  │    │  Studio AI   │
│              │    │   브라우저)   │    │  안목 AI     │
└──────┬───────┘    └──────┬───────┘    └──────┬───────┘
       │                   │                   │
       └───────────────────┴───────────────────┘
                           │
                  WebSocket으로 서버 연결
                  (사람 클라이언트와 같은 프로토콜)
```

### 2.2 통신 흐름

#### 시나리오 1: 두 디바이스 모두 온라인
```
디바이스 A → 디바이스 B 메시지

1. A가 메시지 입력
2. A 클라이언트: 서버에 시그널링 요청 (B의 SDP 받기)
3. B와 WebRTC P2P 연결 시도
4. 성공: P2P DataChannel로 직접 전송 (10~50ms)
5. B가 ACK → A가 "전송됨" 표시
6. 서버는 메시지 자체를 저장하지 않음 (시그널링만 처리)
```

#### 시나리오 2: B가 오프라인
```
1. A가 메시지 입력
2. A 클라이언트: 서버에 메시지 전송 (POST /api/messages)
3. 서버: pending_messages에 INSERT (target_device_id = B)
4. 서버: B의 FCM 토큰으로 푸시 알림 발송
5. B가 앱 켬 (또는 푸시 탭)
6. B 클라이언트: 서버에 "내 미수신 메시지 줘" (GET /api/messages/pending)
7. B 받음 → ACK
8. 서버: pending_messages에서 해당 row 삭제
9. 1주일 후 만료 cron이 미배달 메시지 정리
```

#### 시나리오 3: 같은 사용자의 다른 디바이스 동기화
```
A 폰에서 메시지 보냄
  ↓
A의 다른 디바이스 (A PC, A 태블릿)에도 같은 메시지 표시되어야 함
  ↓
서버: A의 모든 디바이스 목록 조회 → 보낸 디바이스 제외 → 나머지에 broadcast
  - A 폰 (보낸 디바이스): 즉시 자기 화면에 표시
  - A PC: 서버에서 받음
  - A 태블릿: 오프라인이면 큐에 저장
```

---

## 3. 데이터 모델

### 3.1 서버 DB (`server.db`)

```sql
-- 사용자 (Phase 1에서는 사실상 1명)
CREATE TABLE users (
    user_id          TEXT PRIMARY KEY,        -- "lhs"
    name             TEXT NOT NULL,            -- "이효승"
    phone            TEXT,                     -- "+821012345678" (Phase 1에서는 NULL)
    profile_image    TEXT,                     -- URL
    status_message   TEXT,
    is_ai            INTEGER DEFAULT 0,        -- 0=사람, 1=AI
    created_at       INTEGER NOT NULL,
    last_seen        INTEGER
);

CREATE INDEX idx_users_phone ON users(phone);

-- 디바이스 (계정당 N개)
CREATE TABLE devices (
    device_id        TEXT PRIMARY KEY,         -- UUID
    user_id          TEXT NOT NULL,
    device_name      TEXT NOT NULL,            -- "비손피씨" / "안목" / "안목 폰"
    device_type      TEXT NOT NULL,            -- "windows" / "android" / "linux" / "web"
    fcm_token        TEXT,                     -- Android만
    last_seen        INTEGER,
    is_online        INTEGER DEFAULT 0,
    online_addr      TEXT,                     -- WebSocket 연결 정보 (라우팅용)
    created_at       INTEGER NOT NULL,
    FOREIGN KEY (user_id) REFERENCES users(user_id)
);

CREATE INDEX idx_devices_user ON devices(user_id);
CREATE INDEX idx_devices_online ON devices(is_online);

-- 채팅방 (1:1 또는 그룹)
CREATE TABLE rooms (
    room_id          TEXT PRIMARY KEY,         -- UUID
    room_type        TEXT NOT NULL,            -- "direct" / "group"
    name             TEXT,                     -- 그룹만
    creator_user_id  TEXT,
    created_at       INTEGER NOT NULL
);

CREATE TABLE room_members (
    room_id          TEXT NOT NULL,
    user_id          TEXT NOT NULL,
    role             TEXT DEFAULT 'member',    -- 'admin' / 'member'
    joined_at        INTEGER NOT NULL,
    PRIMARY KEY (room_id, user_id)
);

-- 메시지 큐 (배달 대기 중인 메시지, 임시 저장)
CREATE TABLE pending_messages (
    msg_id           TEXT NOT NULL,            -- 글로벌 메시지 ID (ULID)
    target_device_id TEXT NOT NULL,            -- 받을 디바이스
    from_user_id     TEXT NOT NULL,
    from_device_id   TEXT NOT NULL,
    room_id          TEXT NOT NULL,
    server_seq       INTEGER NOT NULL,         -- 전역 순번 (정렬 기준)
    payload          BLOB NOT NULL,            -- 암호화 가능 (Phase 2)
    has_attachment   INTEGER DEFAULT 0,
    attachment_id    TEXT,                     -- pending_files 참조
    created_at       INTEGER NOT NULL,
    expires_at       INTEGER NOT NULL,         -- created_at + 7일
    PRIMARY KEY (msg_id, target_device_id)
);

CREATE INDEX idx_pending_target ON pending_messages(target_device_id);
CREATE INDEX idx_pending_expires ON pending_messages(expires_at);

-- 첨부 파일 임시 저장
CREATE TABLE pending_files (
    file_id          TEXT PRIMARY KEY,         -- UUID
    msg_id           TEXT NOT NULL,
    from_user_id     TEXT NOT NULL,
    filename         TEXT NOT NULL,
    mime             TEXT,
    size             INTEGER,
    storage_path     TEXT NOT NULL,            -- /var/Noah/files/abc.bin
    created_at       INTEGER NOT NULL,
    expires_at       INTEGER NOT NULL,         -- 7일
    download_count   INTEGER DEFAULT 0
);

-- 글로벌 sequence 카운터 (server_seq 발급용)
CREATE TABLE seq_counter (
    name             TEXT PRIMARY KEY,         -- "global_message_seq"
    value            INTEGER NOT NULL DEFAULT 0
);
INSERT INTO seq_counter VALUES ('global_message_seq', 0);

-- AI 에이전트 토큰 (Phase 1B)
CREATE TABLE ai_tokens (
    token            TEXT PRIMARY KEY,         -- 랜덤 64자
    user_id          TEXT NOT NULL,            -- AI의 user_id
    name             TEXT,                     -- 토큰 이름
    permissions      TEXT,                     -- JSON
    created_at       INTEGER NOT NULL,
    last_used        INTEGER,
    FOREIGN KEY (user_id) REFERENCES users(user_id)
);
```

### 3.2 클라이언트 DB (모바일/PC 동일)

#### `messages.db` — 가벼움, 항상 메모리에 캐시

```sql
CREATE TABLE messages (
    msg_id              TEXT PRIMARY KEY,        -- ULID
    server_seq          INTEGER,                 -- 서버에서 받은 후 채워짐
    room_id             TEXT NOT NULL,
    from_user_id        TEXT NOT NULL,
    from_device_id      TEXT NOT NULL,
    msg_type            TEXT NOT NULL,           -- 'text' / 'image' / 'video' / 'file' / 'system'
    text                TEXT,                    -- 메시지 본문 (텍스트 또는 첨부 캡션)
    has_attachment      INTEGER DEFAULT 0,
    attachment_id       TEXT,                    -- attachments.db 참조
    client_timestamp    INTEGER NOT NULL,        -- 보낸/받은 시각 (로컬)
    server_timestamp    INTEGER,                 -- 서버 시각
    delivery_status     TEXT NOT NULL,           -- 'sending' / 'sent' / 'delivered' / 'read' / 'failed'
    is_outgoing         INTEGER NOT NULL,        -- 1=내가 보낸, 0=받은
    edited              INTEGER DEFAULT 0,
    deleted             INTEGER DEFAULT 0,
    reply_to_msg_id     TEXT,                    -- 답장 시
    created_at          INTEGER NOT NULL
);

CREATE INDEX idx_msg_room_seq ON messages(room_id, server_seq);
CREATE INDEX idx_msg_room_time ON messages(room_id, client_timestamp);
CREATE INDEX idx_msg_seq ON messages(server_seq);

-- 채팅방 목록
CREATE TABLE rooms (
    room_id             TEXT PRIMARY KEY,
    room_type           TEXT NOT NULL,           -- 'direct' / 'group'
    display_name        TEXT NOT NULL,
    avatar_url          TEXT,
    last_msg_id         TEXT,
    last_msg_text       TEXT,                    -- 미리보기용
    last_msg_at         INTEGER,
    unread_count        INTEGER DEFAULT 0,
    is_pinned           INTEGER DEFAULT 0,
    is_muted            INTEGER DEFAULT 0,
    created_at          INTEGER NOT NULL,
    updated_at          INTEGER NOT NULL
);

CREATE INDEX idx_rooms_updated ON rooms(updated_at DESC);

-- 채팅방 참여자 (로컬 캐시)
CREATE TABLE room_members (
    room_id             TEXT NOT NULL,
    user_id             TEXT NOT NULL,
    user_name           TEXT,
    user_avatar         TEXT,
    is_ai               INTEGER DEFAULT 0,
    PRIMARY KEY (room_id, user_id)
);

-- sync 상태 (각 채팅방별 마지막 동기화 시점)
CREATE TABLE sync_state (
    room_id             TEXT PRIMARY KEY,
    last_seq            INTEGER NOT NULL DEFAULT 0,
    last_synced_at      INTEGER NOT NULL DEFAULT 0
);

-- 이 디바이스 정보
CREATE TABLE this_device (
    key                 TEXT PRIMARY KEY,        -- 'device_id', 'user_id', 'device_name', 'fcm_token'
    value               TEXT
);
```

#### `attachments.db` — 무거움, 별도 분리

```sql
CREATE TABLE attachments (
    attachment_id       TEXT PRIMARY KEY,        -- UUID
    msg_id              TEXT NOT NULL,           -- 어느 메시지의 첨부
    filename            TEXT NOT NULL,
    mime                TEXT,
    size                INTEGER,
    width               INTEGER,                 -- 이미지/비디오
    height              INTEGER,
    duration_sec        INTEGER,                 -- 비디오/오디오
    file_path           TEXT,                    -- 로컬 경로 (없으면 미다운로드)
    thumbnail_path      TEXT,                    -- 작은 썸네일 경로
    sha256              TEXT,
    download_status     TEXT DEFAULT 'pending',  -- 'pending' / 'downloading' / 'completed' / 'failed'
    download_progress   INTEGER DEFAULT 0,       -- 0~100
    created_at          INTEGER NOT NULL
);

CREATE INDEX idx_att_msg ON attachments(msg_id);
CREATE INDEX idx_att_status ON attachments(download_status);
```

#### 첨부 파일 폴더 구조

```
~/Noah_data/
└── attachments/
    ├── 2026/
    │   ├── 04/
    │   │   ├── 13/
    │   │   │   ├── original/
    │   │   │   │   ├── abc123.jpg          ← 원본 파일
    │   │   │   │   ├── def456.pdf
    │   │   │   │   └── ghi789.mp4
    │   │   │   └── thumbnail/
    │   │   │       ├── abc123_thumb.jpg    ← 200x200 썸네일
    │   │   │       └── ghi789_thumb.jpg
    │   │   └── 14/
    │   │       └── ...
    │   └── 05/
    └── ...
```

날짜별 분류로 관리. 오래된 첨부 자동 삭제 가능.

---

## 4. API 명세 (서버)

### 4.1 인증 / 디바이스

```
POST   /api/devices/register
       Body: { user_id, device_name, device_type, fcm_token? }
       → { device_id, token }

GET    /api/devices
       Headers: Authorization: Bearer <token>
       → [{ device_id, name, type, last_seen, is_online }]

DELETE /api/devices/{device_id}
       → 원격 로그아웃

POST   /api/devices/{device_id}/heartbeat
       → 온라인 상태 업데이트
```

### 4.2 채팅방

```
GET    /api/rooms
       → [{ room_id, type, name, last_msg, unread_count }]

POST   /api/rooms/direct
       Body: { other_user_id }
       → { room_id }

POST   /api/rooms/group
       Body: { name, member_user_ids }
       → { room_id }

GET    /api/rooms/{room_id}/members
       → [{ user_id, name, role }]
```

### 4.3 메시지

```
POST   /api/messages
       Body: {
         msg_id,                // 클라가 생성한 ULID
         room_id,
         payload,                // 텍스트 또는 첨부 메타
         attachment_id?,         // 첨부 있으면
         client_timestamp
       }
       → {
         server_seq,             // 서버가 발급한 순번
         server_timestamp,
         status: "queued"
       }

GET    /api/messages/pending
       Headers: Device-Id: <device_id>
       → [{ msg_id, room_id, payload, ... }]

POST   /api/messages/ack
       Body: { msg_ids: [...] }
       → { acked: N }
```

### 4.4 첨부 파일

```
POST   /api/files/upload
       Multipart: file
       → { file_id, url, expires_at }

GET    /api/files/{file_id}
       → 파일 다운로드 (스트리밍)

DELETE /api/files/{file_id}
       → 명시적 삭제 (보통 ACK 시 자동 삭제)
```

### 4.5 WebRTC 시그널링 (WebSocket)

```
WS   /ws

클라 → 서버:
  { type: "auth", token: "..." }
  { type: "rtc_offer", target_device_id, sdp }
  { type: "rtc_answer", target_device_id, sdp }
  { type: "rtc_ice", target_device_id, candidate }
  { type: "rtc_close", target_device_id }

서버 → 클라:
  { type: "auth_ok" }
  { type: "new_message", msg_id, room_id, ... }   // 큐에 메시지 들어왔을 때
  { type: "rtc_offer", from_device_id, sdp }
  { type: "rtc_answer", from_device_id, sdp }
  { type: "rtc_ice", from_device_id, candidate }
  { type: "device_online", device_id }
  { type: "device_offline", device_id }
```

### 4.6 P2P DataChannel 메시지 (WebRTC)

서버를 거치지 않는 메시지 형식:

```javascript
// 메시지 전송
{
  type: "message",
  msg_id: "01HX...",
  payload: { text: "안녕" },
  client_timestamp: 1726000000000
}

// ACK
{
  type: "ack",
  msg_id: "01HX..."
}

// 파일 청크
{
  type: "file_chunk",
  attachment_id: "abc",
  chunk_index: 5,
  total_chunks: 100,
  data: "<base64>"
}
```

---

## 5. 클라이언트 빌드 분리 (디바이스별 EXE)

### 5.1 핵심 원리

같은 코드 베이스, 빌드 시 `AssemblyName`만 다르게 → 진짜 다른 EXE.

```
소스: C:\WindowsApp\Noah\
빌드 출력:
  - publish\BisonPC\Noah_BisonPC.exe
  - publish\Anmok\Noah_Anmok.exe
  - publish\Crew\Noah_Crew.exe
  - ...
```

각 EXE가 자기 이름으로 데이터 폴더 자동 결정.

### 5.2 csproj 설정

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>WinExe</OutputType>
    <TargetFramework>net8.0-windows</TargetFramework>
    <UseWPF>true</UseWPF>
    <Nullable>enable</Nullable>
    
    <!-- 빌드 인자로 변경 가능 -->
    <AssemblyName Condition="'$(AssemblyName)' == ''">Noah</AssemblyName>
    <RootNamespace>Noah</RootNamespace>
    <ApplicationIcon Condition="'$(ApplicationIcon)' == ''">Resources\default.ico</ApplicationIcon>
  </PropertyGroup>
</Project>
```

### 5.3 빌드 스크립트 (`build_all.bat`)

```bat
@echo off
echo Building Noah_BisonPC...
dotnet publish -c Release ^
    -p:AssemblyName=Noah_BisonPC ^
    -p:ApplicationIcon=Resources\bison_pc.ico ^
    -o publish\BisonPC

echo Building Noah_Anmok...
dotnet publish -c Release ^
    -p:AssemblyName=Noah_Anmok ^
    -p:ApplicationIcon=Resources\anmok.ico ^
    -o publish\Anmok

echo Building Noah_Crew...
dotnet publish -c Release ^
    -p:AssemblyName=Noah_Crew ^
    -p:ApplicationIcon=Resources\crew.ico ^
    -o publish\Crew

echo Done.
```

### 5.4 자동 디바이스 설정 코드

```csharp
// AppInfo.cs
public static class AppInfo
{
    public static string AssemblyName => 
        Assembly.GetExecutingAssembly().GetName().Name ?? "Noah";
    
    public static string DeviceName => AssemblyName switch
    {
        "Noah_BisonPC" => "비손피씨",
        "Noah_Anmok"   => "안목",
        "Noah_Crew"    => "크루",
        _                => Environment.MachineName
    };
    
    public static string DataPath
    {
        get
        {
            var path = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                AssemblyName);
            Directory.CreateDirectory(path);
            return path;
        }
    }
    
    public static string DeviceId
    {
        get
        {
            var path = Path.Combine(DataPath, "device_id.txt");
            if (File.Exists(path))
                return File.ReadAllText(path).Trim();
            
            var id = Guid.NewGuid().ToString();
            File.WriteAllText(path, id);
            File.SetAttributes(path, FileAttributes.ReadOnly | FileAttributes.Hidden);
            return id;
        }
    }
}
```

### 5.5 결과

```
C:\Users\lhs\AppData\Local\
├── Noah_BisonPC\
│   ├── device_id.txt          ← UUID 1
│   ├── messages.db
│   ├── attachments.db
│   └── attachments\
│       └── 2026\04\13\...
├── Noah_Anmok\
│   ├── device_id.txt          ← UUID 2 (다름)
│   ├── messages.db
│   └── ...
└── Noah_Crew\
    ├── device_id.txt          ← UUID 3
    └── ...
```

각 EXE 더블클릭하면 자기 데이터 폴더 사용. 동시 실행 가능. 부팅 후에도 영구 유지.

---

## 6. UI 디자인 (카카오톡 흉내)

### 6.1 라이브러리

```
모바일 (MAUI):
  - Syncfusion.Maui.Chat (Community License, 무료)
  - 채팅 UI 80% 자동 처리

PC (WPF):
  - HandyControl (MIT, 무료)
  - 채팅 컴포넌트 + 직접 커스터마이징
```

### 6.2 색상 팔레트 (카카오톡 매칭)

```
배경:
  - 채팅방 배경:    #B2C7D9
  - 헤더 배경:      #B2C7D9
  - 입력바 배경:    #FFFFFF

메시지 버블:
  - 내 메시지:      #FFE812 (카카오 노랑)
  - 상대 메시지:    #FFFFFF
  - 텍스트:         #000000
  - 시간:           #999999

기타:
  - 안 읽음 표시:   #FFD500
  - 액센트:         #FFE812
  - 시스템 메시지:  #C0CCD8 배경, #4A5C6E 텍스트
```

### 6.3 화면 구성

#### 6.3.1 채팅방 목록 (모바일/PC 공통)

```
┌─────────────────────────────────┐
│  ☰  Noah           🔍  ⚙     │  헤더
├─────────────────────────────────┤
│  ┌──┐                           │
│  │👤│ NeoStock팀          ⓡ12  │  ← 그룹 채팅
│  └──┘ 크루: Phase 7 시작...     │
│       오후 3:42                 │
├─────────────────────────────────┤
│  ┌──┐                           │
│  │👤│ 비손피씨            ⓡ3   │  ← 1:1 (디바이스)
│  └──┘ 빌드 완료                 │
│       오후 3:30                 │
├─────────────────────────────────┤
│  ┌──┐                           │
│  │👤│ 안목 (모바일)            │  ← 안 읽음 없음
│  └──┘ ✅ 설치 완료              │
│       오후 3:15                 │
├─────────────────────────────────┤
│  채팅 │ 친구 │ 더보기            │  하단 탭 (모바일만)
└─────────────────────────────────┘
```

#### 6.3.2 채팅방 (1:1 또는 그룹)

```
┌─────────────────────────────────┐
│  ←   NeoStock팀          ☰     │  헤더
├─────────────────────────────────┤
│         ─── 오늘 ───             │
│                                 │
│  ┌──┐                           │
│  │크│ 크루                     │
│  └──┘ ┌─────────────────┐     │
│       │ Phase 7 시작합시다 │     │  ← 상대 메시지
│       └─────────────────┘     │
│       오후 3:40                 │
│                                 │
│  ┌──┐                           │
│  │비│ 비손피씨                 │
│  └──┘ ┌─────────────────┐     │
│       │ ✅ 빌드 완료      │     │
│       └─────────────────┘     │
│       오후 3:42                 │
│                                 │
│            ┌────────────────┐ │
│            │ 확인했습니다    │ │  ← 내 메시지
│            └────────────────┘ │
│                  오후 3:43   1 │  ← 안 읽음 1
├─────────────────────────────────┤
│  ➕  메시지 입력...      😀  ➤ │  입력바
└─────────────────────────────────┘
```

#### 6.3.3 첨부 미리보기

```
┌─────────────────────────────────┐
│  ┌──┐                           │
│  │비│ 비손피씨                 │
│  └──┘ ┌─────────────────┐     │
│       │  ┌───────────┐  │     │
│       │  │  📷 thumb │  │     │  ← 이미지 썸네일
│       │  └───────────┘  │     │
│       │  IMG_1234.jpg   │     │
│       │  2.4 MB         │     │
│       └─────────────────┘     │
│       오후 3:45                 │
│                                 │
│  ┌──┐                           │
│  │비│ 비손피씨                 │
│  └──┘ ┌─────────────────┐     │
│       │  📄 report.pdf  │     │  ← 문서 (아이콘만)
│       │  1.2 MB         │     │
│       │  [다운로드]      │     │
│       └─────────────────┘     │
└─────────────────────────────────┘
```

### 6.4 메시지 입력 동작

- **OS 기본 키보드**: 한국어 천지인/쿼티/나랏글, 음성입력, 이모지 키보드 모두 그대로
- **첨부 버튼 (➕)**: 카메라, 갤러리, 파일, 위치 등 메뉴
- **이모지 버튼 (😀)**: OS 이모지 키보드 호출
- **전송 버튼 (➤)**: 텍스트 있을 때만 활성화
- **음성 메시지**: 길게 누르기 (Phase 2)
- **답장**: 메시지 길게 누르기 → 답장 옵션 (Phase 2)

---

## 7. 작업 분담

### 7.1 서버 (비손서버, Linux)

**환경**: `/home/godinus/Noah/`
**언어**: Node.js 20+
**DB**: SQLite (Phase 1), Postgres (Phase 3 확장 시)

**작업**:
1. Express 서버 + WebSocket (`ws` 라이브러리)
2. SQLite 스키마 생성 (위 3.1 그대로)
3. REST API 구현 (위 4번)
4. WebRTC 시그널링 핸들러
5. 메시지 큐 + ACK + 7일 만료 cron
6. FCM 푸시 발송 (firebase-admin)
7. 첨부 파일 저장 + 다운로드 + 만료 삭제
8. server_seq 발급 로직 (트랜잭션)
9. AI 토큰 발급/검증
10. 로깅 + 헬스체크

**예상 시간**: Phase 1A 12h + Phase 1B AI 통합 4h = 16h

### 7.2 모바일 (안목, Android Studio)

**환경**: `C:\MobileApp\Noah\`
**언어**: C# MAUI net9.0-android
**UI**: Syncfusion.Maui.Chat

**작업**:
1. MAUI 프로젝트 초기화 + Syncfusion 설치
2. 데이터 모델 + SQLite (`messages.db`, `attachments.db`)
3. 메인 화면 (채팅방 목록)
4. 채팅방 화면 (Syncfusion SfChat 커스터마이징)
5. WebSocket 클라이언트 (websocket-sharp 또는 직접)
6. WebRTC 클라이언트 (SimplePeer 포팅 또는 native)
7. 첨부 처리 (카메라, 갤러리, 파일, 썸네일 생성)
8. FCM 수신 + 알림 표시 + 배지 (ShortcutBadger)
9. 디바이스 등록 + DeviceId 영구 저장
10. 한국어 키보드 검증 (Entry Keyboard="Chat")

**예상 시간**: 16h

### 7.3 PC (비손피씨, Windows)

**환경**: `C:\WindowsApp\Noah\`
**언어**: C# WPF net8.0-windows
**UI**: HandyControl + 커스텀

**작업**:
1. WPF 프로젝트 초기화 + HandyControl 설치
2. 데이터 모델 + SQLite (모바일과 동일)
3. 빌드 분리 시스템 (`AssemblyName` 기반)
4. AppInfo 클래스 (DeviceName, DataPath, DeviceId 자동)
5. 메인 윈도우 (채팅방 목록)
6. 채팅방 윈도우 (커스텀 메시지 버블)
7. WebSocket 클라이언트
8. WebRTC 클라이언트 (SipSorcery)
9. 첨부 처리 (드래그앤드롭 포함)
10. Windows Toast 알림 + 작업표시줄 배지 (TaskbarItemInfo)
11. 시스템 트레이 + 자동 시작
12. 빌드 스크립트 (`build_all.bat`)

**예상 시간**: 18h

### 7.4 크루 (지휘 + 코드 작성)

**작업**:
1. 설계 문서 유지보수
2. 비손서버/안목/비손피씨 작업 의뢰 + 리뷰
3. 공통 코드 작성 (데이터 모델, 프로토콜 정의)
4. 통합 테스트 시나리오 작성
5. 트러블슈팅

---

## 8. Phase 분할 및 일정

### Phase 1A — 사람 간 메신저 (1주, ~28h)

```
[목표]
사람만 사용. 1:1 채팅. 기본 기능 동작.

[기능]
✅ 디바이스 등록 (별도 EXE 빌드)
✅ 1:1 채팅
✅ 텍스트 메시지
✅ 첨부 파일 (이미지/문서)
✅ WebRTC P2P + 서버 폴백
✅ 디바이스 간 동기화
✅ 메시지 시간순 정렬 (server_seq)
✅ FCM 푸시 (모바일)
✅ Toast 알림 (PC)
✅ 배지 (가능한 단말)
✅ 카카오톡 스타일 UI

[제외]
❌ 사용자 인증 (모두 "이효승")
❌ 그룹 채팅
❌ AI 에이전트
❌ DB sync (디바이스 간 히스토리 복구)
❌ E2E 암호화

[일정 가정]
- 비손서버: 2일 (12h)
- 안목: 2일 (16h)
- 비손피씨: 2.5일 (18h)
- 통합 테스트: 0.5일 (4h)
- 합계: 약 1주 (병렬 작업)
```

### Phase 1B — AI 에이전트 통합 (3일, ~14h)

```
[추가 기능]
✅ 그룹 채팅
✅ AI 에이전트 등록 (크루, 안목, 비손서버, 비손피씨)
✅ AI 클라이언트 SDK (Python, Node.js)
✅ 사람 + AI 혼합 채팅방 ([NeoStock 개발팀] 등)
✅ 기존 git push 워크플로 → 메신저로 전환

[작업]
- 비손서버: AI 토큰 + 그룹 라우팅 (4h)
- 모바일/PC: 그룹 채팅 UI (각 3h)
- AI SDK Python (비손서버용, 2h)
- AI SDK Node.js (크루용, 2h)
```

### Phase 2 — 디바이스 간 P2P DB Sync (1주, ~12h)

```
[기능]
✅ 새 디바이스가 기존 디바이스에서 P2P로 히스토리 가져오기
✅ 증분 sync (last_seq 기반)
✅ 디바이스 승인 UI
✅ 무결성 검증 (해시)
✅ 첨부 파일 lazy sync

[목표]
서버는 절대 메시지 영구 저장 안 함.
새 PC 설치하면 폰에서 P2P로 1만 메시지 가져옴.
```

### Phase 3 — 보안 강화 (선택, 1주)

```
✅ E2E 암호화 (RSA 또는 Signal Protocol)
✅ 디바이스별 키쌍
✅ 메시지 서명 + 검증
✅ 암호화된 클라우드 백업
✅ SMS 전화번호 인증 (선택)
```

### Phase 4+ — 기능 확장 (지속)

```
- 음성/영상 통화 (WebRTC AV)
- 메시지 검색
- 이모지 반응
- 답장 인용
- 메시지 편집/삭제
- 친구 추천
- 프로필 사진
- 상태 메시지
- 알림 설정
```

---

## 9. 기술 스택

### 9.1 서버

```yaml
runtime: Node.js 20+ LTS
framework: Express 4.x
websocket: ws 8.x
db: better-sqlite3 (Phase 1), pg (Phase 3)
push: firebase-admin
file: multer + fs
auth: jsonwebtoken
crypto: crypto (built-in)
logging: winston
process: pm2
```

### 9.2 모바일

```yaml
sdk: .NET 9 MAUI
target: net9.0-android
db: SQLitePCLRaw + Microsoft.Data.Sqlite
ui: Syncfusion.Maui.Chat (Community License)
mvvm: CommunityToolkit.Mvvm
http: HttpClient
ws: websocket-sharp 또는 System.Net.WebSockets
webrtc: TBD (WebView2 + JS 또는 native plugin)
fcm: Plugin.Firebase.CloudMessaging
notification: Plugin.LocalNotification
permissions: Microsoft.Maui.ApplicationModel
contacts: Plugin.ContactService
camera: MediaPicker
storage: FileSystem
```

### 9.3 PC

```yaml
framework: .NET 8 WPF
target: net8.0-windows
db: SQLitePCLRaw + Microsoft.Data.Sqlite
ui: HandyControl 3.x
mvvm: CommunityToolkit.Mvvm
http: HttpClient
ws: System.Net.WebSockets
webrtc: SipSorcery
notification: Microsoft.Toolkit.Uwp.Notifications
tray: Hardcodet.NotifyIcon.Wpf
icon: ApplicationIcon (csproj)
```

---

## 10. 폴더 구조

### 10.1 서버

```
~/Noah/                          ← Linux
├── package.json
├── server.js                      ← 메인
├── config.js
├── routes/
│   ├── auth.js
│   ├── devices.js
│   ├── messages.js
│   ├── files.js
│   └── rooms.js
├── services/
│   ├── webrtc_signaling.js
│   ├── fcm_push.js
│   ├── message_queue.js
│   ├── attachment_store.js
│   ├── seq_counter.js
│   └── cleanup_cron.js
├── db/
│   ├── schema.sql
│   ├── server.db                  ← SQLite
│   └── migrations/
├── data/
│   └── attachments/               ← 첨부 임시 저장
│       └── 2026/04/13/...
├── logs/
└── README.md
```

### 10.2 모바일 (안목)

```
C:\MobileApp\Noah\               ← Windows에서 작업
├── Noah.csproj
├── MauiProgram.cs
├── App.xaml
├── AppShell.xaml
├── Models/
│   ├── User.cs
│   ├── Device.cs
│   ├── Room.cs
│   ├── Message.cs
│   └── Attachment.cs
├── Data/
│   ├── MessagesDb.cs
│   ├── AttachmentsDb.cs
│   ├── DbInitializer.cs
│   └── Repositories/
│       ├── MessageRepository.cs
│       ├── RoomRepository.cs
│       └── AttachmentRepository.cs
├── Services/
│   ├── ApiClient.cs               ← REST API
│   ├── WebSocketService.cs
│   ├── WebRtcService.cs
│   ├── ChatService.cs             ← 송수신 통합
│   ├── SyncService.cs
│   ├── AttachmentService.cs
│   ├── FcmService.cs
│   ├── BadgeService.cs
│   ├── DeviceService.cs
│   └── ContactService.cs
├── ViewModels/
│   ├── ChatListViewModel.cs
│   ├── ChatRoomViewModel.cs
│   ├── ContactsViewModel.cs
│   └── SettingsViewModel.cs
├── Views/
│   ├── ChatListPage.xaml
│   ├── ChatRoomPage.xaml
│   ├── ContactsPage.xaml
│   └── SettingsPage.xaml
├── Resources/
│   ├── AppIcon/
│   ├── Images/
│   └── Styles/
├── Platforms/
│   └── Android/
│       ├── AndroidManifest.xml
│       └── MainActivity.cs
└── README.md

런타임 데이터:
~/Noah_data/  (FileSystem.AppDataDirectory)
├── messages.db
├── attachments.db
└── attachments/
    └── 2026/04/13/...
```

### 10.3 PC (비손피씨)

```
C:\WindowsApp\Noah\
├── Noah.csproj
├── App.xaml
├── App.xaml.cs
├── AppInfo.cs                     ← DeviceName, DataPath, DeviceId
├── MainWindow.xaml
├── Models/                        ← (모바일과 동일 구조)
├── Data/
├── Services/
├── ViewModels/
├── Views/
│   ├── ChatListPage.xaml
│   ├── ChatRoomPage.xaml
│   ├── DevicesPage.xaml
│   └── SettingsPage.xaml
├── Resources/
│   ├── default.ico
│   ├── bison_pc.ico               ← 비손피씨 빌드용
│   ├── anmok.ico                  ← 안목 빌드용
│   └── crew.ico                   ← 크루 빌드용
├── build_all.bat                  ← 빌드 자동화
└── publish/                       ← 빌드 결과
    ├── BisonPC/
    │   └── Noah_BisonPC.exe
    ├── Anmok/
    │   └── Noah_Anmok.exe
    └── Crew/
        └── Noah_Crew.exe

런타임 데이터:
%LOCALAPPDATA%\Noah_BisonPC\
├── device_id.txt
├── messages.db
├── attachments.db
└── attachments\

%LOCALAPPDATA%\Noah_Anmok\
└── (별개)
```

---

## 11. 위험 관리

| 위험 | 확률 | 영향 | 대응 |
|------|------|------|------|
| WebRTC NAT 통과 실패 | 30% | 중 | 서버 폴백 (자동) |
| Syncfusion 라이선스 거절 | 5% | 중 | HandyControl로 전환 |
| FCM 푸시 지연 | 20% | 낮 | 평균 1초 이내 (정상) |
| Android 배지 안 보임 | 70% | 낮 | 알림 자체는 보임 |
| SQLite 동시성 (write 충돌) | 10% | 중 | WAL 모드 + 트랜잭션 |
| 첨부 파일 메모리 폭발 | 30% | 높 | 청크 스트리밍 |
| 메시지 순서 어긋남 | 15% | 중 | server_seq 정렬 |
| 디바이스 ID 손실 | 5% | 높 | 3중 백업 |
| LiveCharts2 같은 RC 버그 | - | - | 안정 라이브러리만 사용 |
| 서버 다운 | 5% | 높 | systemd auto-restart + 로그 |
| 디스크 가득 (첨부) | 10% | 중 | 7일 만료 cron |

---

## 12. 사용자 액션 (이효승)

설계 단계가 끝나면 사용자가 직접 해야 할 일:

### 12.1 GitHub 레포 생성

```
1. GitHub 로그인
2. New Repository
3. Repository name: Noah
4. Private (권장)
5. Initialize with README
6. 생성

URL: https://github.com/godinus123/Noah
```

크루는 GitHub API 막혀있어서 직접 못 만듭니다. 사용자가 만들고 `Noah` 이름 알려주세요.

### 12.2 Syncfusion Community License

```
1. https://www.syncfusion.com/sales/communitylicense
2. 가입 (이메일만)
3. License Key 받기
4. MAUI 프로젝트에 등록 (코드 1줄)

대안: 라이선스 신청 안 하면 워터마크 표시됨 (개발용으로 OK)
```

### 12.3 Firebase 프로젝트 (Phase 1A 후반)

```
1. https://console.firebase.google.com
2. 새 프로젝트 (이름: Noah)
3. Android 앱 추가 (패키지: com.noah.anmok)
4. google-services.json 다운로드
5. MAUI 프로젝트의 Platforms/Android/에 복사
6. Firebase Admin SDK 키 (서버용) 다운로드
7. 비손서버에 전달
```

### 12.4 도메인/HTTPS (선택)

```
1. ngrok URL 사용 (현재 NeoStock과 같음, 무료)
2. 또는 자체 도메인 + Let's Encrypt
3. 또는 Cloudflare Tunnel
```

---

## 13. 시작 체크리스트

본격 구현 시작 전 확인:

```
[설계]
✅ 데이터 모델 확정
✅ API 명세 확정
✅ 빌드 분리 방식 확정
✅ UI 라이브러리 선택
✅ 폴더 구조 확정
✅ Phase 분할 확정

[사용자 액션]
□ GitHub 레포 godinus123/Noah 생성
□ Syncfusion Community License 신청
□ Firebase 프로젝트 (Phase 1A 후반)
□ /home/godinus/Noah/ 폴더 생성 의뢰

[비손서버 준비]
□ Node.js 20+ 설치 확인
□ SQLite 라이브러리 (better-sqlite3)
□ pm2 설치
□ ngrok 또는 도메인 준비

[안목 준비]
□ MAUI 워크로드 설치 (dotnet workload install maui)
□ Syncfusion 라이선스 등록
□ Android SDK + ADB
□ 테스트 폰 1대

[비손피씨 준비]
□ Visual Studio 2022 또는 Rider
□ HandyControl NuGet
□ Windows 10/11 SDK
```

---

## 14. 다음 액션

이 설계 문서가 OK라는 가정 하에:

```
1. crew-channel에 비손서버 작업 의뢰 push
   - /home/godinus/Noah/ 폴더 생성
   - Phase 7 끝나면 Phase 1A 시작 안내
   
2. NeoStock channel에 안목/비손피씨 안내
   - Phase 7 종료 후 Noah로 이동
   - 사전 환경 셋업 안내
   
3. 사용자 액션 가이드 push
   - GitHub 레포 만들기
   - Syncfusion 가입
   - Firebase 가입 (Phase 1A 후반)
   
4. NeoStock Phase 7 진행 (안목/비손피씨 작업)
   
5. Phase 7 완료 후 Noah 본격 시작
```

---

## 15. 결론

Noah은 **카카오톡 같은 사용자 경험 + Signal 같은 개인정보 보호 + AI 에이전트 통합**이라는 세 가지 가치를 추구하는 메신저입니다.

핵심 차별점:
1. **서버는 데이터를 영구 저장하지 않음** (배달 후 삭제)
2. **WebRTC P2P로 빠른 직접 통신** (서버 부담 최소)
3. **AI 에이전트가 사람과 같은 채팅방** (자연스러운 협업)
4. **빌드 시 디바이스 분리** (테스트 환경 단순)

Phase 1A 1주 + Phase 1B 3일 = 약 10일이면 동작하는 MVP 완성.

NeoStock Phase 7 완료 후 시작합니다.

---

**문서 작성 완료**: 2026-04-13 01:55 KST
**작성자**: agent_crew
**위치**: `/tmp/Noah/Noah_design.md`
