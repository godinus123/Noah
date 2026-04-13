# [크루→비손피씨] NOAH v0.1 PC 클라이언트 구현 요청

**작성자**: agent_crew
**작성일**: 2026-04-13
**우선순위**: P1 (NeoStock Phase 7 종료 후 시작)
**대상**: 비손피씨 (Windows Visual Studio Claude Code)
**프로젝트**: NOAH Messenger v0.1 — PC Client
**예상 시간**: 24h

---

## 🎯 작업 개요

NOAH v0.1 메신저의 **Windows PC 클라이언트**를 처음부터 구축합니다.

WPF + HandyControl 기반의 카카오톡 스타일 메신저 앱.  
사용자(이효승)가 매일 사용할 도구이므로 **안정성이 최우선**입니다.

---

## 📁 프로젝트 폴더

```powershell
# 이미 NOAH 레포 클론되어 있다면 pull
cd C:\WindowsApp\Noah
git pull origin main

# 아직 없다면 클론
cd C:\WindowsApp
git clone https://github.com/godinus123/Noah.git
cd Noah\pc
```

작업 폴더: **`C:\WindowsApp\Noah\pc`**

⚠️ **주의**: pc/ 폴더는 현재 README.md만 있고 비어 있음. 비손피씨가 처음부터 작성.

---

## 🛠️ 사전 요구사항

```powershell
# .NET 8 SDK 확인
dotnet --version
# 8.0.x 이상

# 미설치 시
winget install Microsoft.DotNet.SDK.8

# Visual Studio 2022 또는 Rider
# (Visual Studio Community 2022 권장)
```

---

## 📋 작업 범위 (24h)

### Phase A: 기초 (6h)
1. WPF 프로젝트 생성 + HandyControl
2. 폴더 구조 셋업
3. AppInfo 클래스 (DataPath, DeviceId 자동)
4. SQLite 연결 (Microsoft.Data.Sqlite)
5. system.db 스키마 생성 (자동)

### Phase B: 인증 (4h)
6. ApiClient 클래스 (HttpClient)
7. 로그인 화면
8. 가입 화면
9. JWT 토큰 저장 (system.db)
10. 자동 로그인 (다음 실행 시)

### Phase C: 메인 (6h)
11. WebSocketClient (자동 재연결, 지수 백오프)
12. 메인 윈도우 (친구 목록)
13. 친구 추가 (사용자 이름 검색)
14. 친구 목록 표시

### Phase D: 채팅 (8h)
15. 채팅방 윈도우 (메시지 버블 UI)
16. 메시지 송수신 (WebSocket)
17. Markdig 마크다운 렌더링
18. 코드 블록 구문 강조 (ColorCode)
19. Ctrl+V 이미지 붙여넣기
20. 드래그앤드롭 파일 첨부
21. 채팅방 닫기 → 저장 다이얼로그
22. .noahdb 파일 생성/로드

---

## 🏗️ 기술 스택

```
Framework:    .NET 8 WPF
UI:           HandyControl 3.x (NuGet)
DB:           Microsoft.Data.Sqlite + Dapper
MVVM:         CommunityToolkit.Mvvm
HTTP:         HttpClient (기본)
WebSocket:    System.Net.WebSockets (기본)
Markdown:     Markdig
Syntax:       ColorCode.Core
Notification: Hardcodet.NotifyIcon.Wpf
Logging:      Serilog
```

NuGet 명령어:
```powershell
dotnet new wpf -n Noah -f net8.0
cd Noah
dotnet add package HandyControl
dotnet add package Microsoft.Data.Sqlite
dotnet add package Dapper
dotnet add package CommunityToolkit.Mvvm
dotnet add package Markdig
dotnet add package ColorCode.Core
dotnet add package Hardcodet.NotifyIcon.Wpf
dotnet add package Serilog
dotnet add package Serilog.Sinks.File
```

---

## 📁 폴더 구조 (목표)

```
pc/
├── README.md               (이미 있음)
├── Noah.sln
├── Noah/
│   ├── Noah.csproj
│   ├── App.xaml
│   ├── App.xaml.cs
│   ├── AppInfo.cs              ← DataPath, DeviceId 자동
│   ├── MainWindow.xaml         ← 메인 (친구 목록)
│   ├── MainWindow.xaml.cs
│   │
│   ├── Models/
│   │   ├── User.cs
│   │   ├── Friend.cs
│   │   ├── Message.cs
│   │   ├── Conversation.cs
│   │   └── Attachment.cs
│   │
│   ├── Data/
│   │   ├── SystemDb.cs         ← system.db
│   │   ├── ConversationDb.cs   ← .noahdb 파일
│   │   └── DbInitializer.cs
│   │
│   ├── Services/
│   │   ├── ApiClient.cs        ← REST API
│   │   ├── WebSocketClient.cs  ← WebSocket + 재연결
│   │   ├── ChatService.cs      ← 메시지 송수신
│   │   ├── ConversationService.cs ← 대화 저장/로드
│   │   ├── AttachmentService.cs   ← 첨부 처리
│   │   ├── MarkdownService.cs     ← Markdig
│   │   └── LogService.cs          ← Serilog
│   │
│   ├── ViewModels/
│   │   ├── LoginViewModel.cs
│   │   ├── RegisterViewModel.cs
│   │   ├── MainViewModel.cs
│   │   ├── ChatRoomViewModel.cs
│   │   └── SettingsViewModel.cs
│   │
│   ├── Views/
│   │   ├── LoginPage.xaml
│   │   ├── RegisterPage.xaml
│   │   ├── ChatRoomWindow.xaml ← 채팅방 (별창)
│   │   ├── SettingsPage.xaml
│   │   └── SaveConversationDialog.xaml
│   │
│   └── Resources/
│       ├── Noah.ico
│       ├── Styles.xaml
│       └── Themes/
│           ├── Light.xaml
│           └── Dark.xaml
│
└── build_all.bat               ← 디바이스별 EXE 빌드 (선택)
```

---

## 📊 데이터 모델

자세한 스키마: [`docs/v0.1_spec.md`](../docs/v0.1_spec.md) 참조.

### system.db (NOAH 본체, 항상 존재)

```sql
-- 본인 정보
CREATE TABLE me (
    key TEXT PRIMARY KEY,
    value TEXT
);
-- 'user_id', 'username', 'token', 'device_id', 'server_url'

-- 친구 목록
CREATE TABLE friends (
    user_id TEXT PRIMARY KEY,
    username TEXT NOT NULL,
    display_name TEXT,
    avatar_url TEXT,
    status_message TEXT,
    last_active INTEGER
);

-- 저장된 대화 인덱스 (.noahdb 파일들)
CREATE TABLE saved_conversations (
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

-- 설정
CREATE TABLE settings (
    key TEXT PRIMARY KEY,
    value TEXT
);
```

### .noahdb (대화 파일, 사용자 지정 위치)

```sql
-- 메타
CREATE TABLE conversation_meta (
    key TEXT PRIMARY KEY,
    value TEXT
);

-- 메시지
CREATE TABLE messages (
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

-- 첨부 (BLOB 직접 - 휴대성)
CREATE TABLE attachments (
    attachment_id TEXT PRIMARY KEY,
    msg_id TEXT NOT NULL,
    filename TEXT,
    mime TEXT,
    size INTEGER,
    data BLOB,
    created_at INTEGER NOT NULL
);
```

---

## 🌐 서버 연결 정보

```
서버 URL: 비손서버에서 받음 (cloudflared/ngrok URL)
포트:     4001 (NeoStock 3001과 충돌 방지)
WebSocket: ws://server:4001/ws
REST:     http://server:4001/api/

테스트용 (로컬):
  http://192.168.x.x:4001  (같은 LAN)
  http://localhost:4001    (같은 PC)
```

비손서버가 NOAH 서버 시작 후 외부 URL 보고하면 그 URL 사용.

설정 화면에서 사용자가 직접 변경 가능하게:
```
설정 → 서버 주소 → [입력 필드]
```

---

## 🎨 UI 디자인 (카카오톡 스타일)

### 색상 (다크 모드 대응)

```xml
<!-- Themes/Light.xaml -->
<Color x:Key="ChatBackground">#B2C7D9</Color>
<Color x:Key="MyMessage">#FFE812</Color>
<Color x:Key="OtherMessage">#FFFFFF</Color>
<Color x:Key="AiMessage">#E8F5E9</Color>
<Color x:Key="TextPrimary">#000000</Color>
<Color x:Key="TextSecondary">#666666</Color>

<!-- Themes/Dark.xaml -->
<Color x:Key="ChatBackground">#1A1A2E</Color>
<Color x:Key="MyMessage">#FFE812</Color>
<Color x:Key="OtherMessage">#2D3748</Color>
<Color x:Key="AiMessage">#1E3A2E</Color>
<Color x:Key="TextPrimary">#FFFFFF</Color>
<Color x:Key="TextSecondary">#AAAAAA</Color>
```

### 화면 구성

```
┌─────────────────────────────┐
│  ☰  NOAH          🔍 ⚙     │  메인 윈도우 (친구 목록)
├─────────────────────────────┤
│  🕊️ 크루 (AI)              │
│  안녕하세요    오후 3:42     │
├─────────────────────────────┤
│  👤 김철수                  │
│  잘 지내?      어제          │
├─────────────────────────────┤
│  + 친구 추가                 │
└─────────────────────────────┘

채팅방 (별창):
┌─────────────────────────────┐
│ ←  🕊️ 크루            ⋮ ✕ │
├─────────────────────────────┤
│ ┌──┐                        │
│ │크│ 크루                  │
│ └──┘ ┌─────────────────┐  │
│      │ 안녕하세요        │  │
│      └─────────────────┘  │
│      오후 3:40             │
│                            │
│           ┌──────────┐    │
│           │ 안녕!     │    │
│           └──────────┘    │
│           오후 3:42        │
├─────────────────────────────┤
│ ➕  메시지 입력...    😀 ➤│
└─────────────────────────────┘
```

---

## ✅ 종료 조건 (Definition of Done)

```
[Phase A: 기초]
[ ] T1.  WPF 프로젝트 빌드 OK
[ ] T2.  HandyControl 적용 (테마)
[ ] T3.  AppInfo.DataPath 정상 (예: %LOCALAPPDATA%\Noah)
[ ] T4.  AppInfo.DeviceId 영구 저장 (재시작 후에도 유지)
[ ] T5.  system.db 자동 생성

[Phase B: 인증]
[ ] T6.  로그인 화면 표시
[ ] T7.  가입 화면 표시
[ ] T8.  POST /api/auth/register → 새 계정 생성
[ ] T9.  POST /api/auth/login → 로그인 + 토큰 저장
[ ] T10. 재시작 → 자동 로그인 (토큰 사용)

[Phase C: 메인]
[ ] T11. WebSocket 연결 + auth
[ ] T12. 자동 재연결 (네트워크 끊김 → 5초 후 재연결)
[ ] T13. 친구 추가 (사용자 이름 검색)
[ ] T14. 친구 목록 표시
[ ] T15. 친구 클릭 → 채팅방 열기

[Phase D: 채팅]
[ ] T16. 텍스트 메시지 송수신
[ ] T17. 메시지 버블 UI (내 메시지 vs 상대 메시지)
[ ] T18. 마크다운 렌더링 (Markdig)
[ ] T19. 코드 블록 구문 강조
[ ] T20. Ctrl+V → 클립보드 이미지 전송
[ ] T21. 드래그앤드롭 → 파일 전송
[ ] T22. 채팅방 X 클릭 → "저장하시겠습니까?" 다이얼로그
[ ] T23. 저장 → .noahdb 파일 생성
[ ] T24. NOAH 재시작 → 저장된 대화 목록 표시
[ ] T25. .noahdb 파일 클릭 → 대화 복원
[ ] T26. @크루 입력 → AI 응답 받기

[안정성]
[ ] T27. 30분 사용 → 크래시 0회
[ ] T28. 메모리 < 200MB
[ ] T29. 네트워크 끊김 → 메시지 큐 → 재연결 후 자동 전송
```

29개 모두 OK → v0.1 PC 클라이언트 완료

---

## 🚧 핵심 구현 포인트

### 1. AppInfo 클래스 (디바이스 식별)

```csharp
// AppInfo.cs
public static class AppInfo
{
    public static string AssemblyName => 
        Assembly.GetExecutingAssembly().GetName().Name ?? "Noah";
    
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

### 2. WebSocketClient (자동 재연결)

```csharp
public class WebSocketClient
{
    private ClientWebSocket _ws;
    private int _retryDelay = 1000;
    private const int MAX_DELAY = 30000;
    private CancellationTokenSource _cts;
    
    public event Action<JsonElement> OnMessage;
    public event Action OnConnected;
    public event Action OnDisconnected;
    
    public async Task ConnectAsync(string url, string token, string deviceId)
    {
        _cts = new CancellationTokenSource();
        _ = Task.Run(() => ConnectWithRetryAsync(url, token, deviceId));
    }
    
    private async Task ConnectWithRetryAsync(string url, string token, string deviceId)
    {
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                _ws = new ClientWebSocket();
                await _ws.ConnectAsync(new Uri(url), _cts.Token);
                
                // 인증
                var authMsg = JsonSerializer.Serialize(new {
                    type = "auth",
                    token,
                    device_id = deviceId
                });
                await SendRawAsync(authMsg);
                
                _retryDelay = 1000;
                OnConnected?.Invoke();
                Log.Info("WebSocket connected");
                
                await ReceiveLoopAsync();
            }
            catch (Exception ex)
            {
                Log.Warn($"Connection failed: {ex.Message}, retry in {_retryDelay}ms");
                OnDisconnected?.Invoke();
                
                await Task.Delay(_retryDelay, _cts.Token);
                _retryDelay = Math.Min(_retryDelay * 2, MAX_DELAY);
            }
        }
    }
    
    // ... ReceiveLoopAsync, SendAsync, etc.
}
```

### 3. 대화 저장 다이얼로그

```csharp
// 채팅방 X 클릭 시
private async void OnClosing(object sender, CancelEventArgs e)
{
    if (_messages.Count == 0) return; // 빈 대화는 그냥 닫기
    
    var dialog = new SaveConversationDialog
    {
        MessageCount = _messages.Count,
        AttachmentCount = _attachments.Count,
        DefaultFileName = $"chat_{DateTime.Now:yyyy-MM-dd}_{_otherUser.Username}.noahdb",
        DefaultFolder = SettingsService.GetDefaultSaveFolder()
    };
    
    var result = dialog.ShowDialog();
    if (result == DialogResult.Save)
    {
        await ConversationService.SaveAsync(dialog.FilePath, _messages, _attachments);
        SystemDb.AddSavedConversation(dialog.FilePath, ...);
    }
    else if (result == DialogResult.Discard)
    {
        // 그냥 닫기 (메모리에서 제거)
    }
    else
    {
        // 취소 = 닫기 취소
        e.Cancel = true;
    }
}
```

### 4. .noahdb 파일 생성

```csharp
public static class ConversationDb
{
    public static async Task SaveAsync(string filePath, 
                                        List<Message> messages, 
                                        List<Attachment> attachments)
    {
        if (File.Exists(filePath)) File.Delete(filePath);
        
        using var conn = new SqliteConnection($"Data Source={filePath}");
        await conn.OpenAsync();
        
        // 스키마 생성
        await conn.ExecuteAsync(SCHEMA_SQL);
        
        // 메타
        await conn.ExecuteAsync(@"
            INSERT INTO conversation_meta (key, value) VALUES 
            ('conv_id', @ConvId),
            ('title', @Title),
            ('created_at', @CreatedAt),
            ('noah_version', '0.1')",
            new { ConvId = Guid.NewGuid(), Title = "...", CreatedAt = DateTime.UtcNow });
        
        // 메시지 일괄 INSERT
        await conn.ExecuteAsync(INSERT_MESSAGE_SQL, messages);
        
        // 첨부 일괄 INSERT (BLOB)
        await conn.ExecuteAsync(INSERT_ATTACHMENT_SQL, attachments);
    }
    
    public static async Task<(List<Message>, List<Attachment>)> LoadAsync(string filePath)
    {
        using var conn = new SqliteConnection($"Data Source={filePath};Mode=ReadOnly");
        await conn.OpenAsync();
        
        var messages = (await conn.QueryAsync<Message>(
            "SELECT * FROM messages ORDER BY server_seq")).ToList();
        
        var attachments = (await conn.QueryAsync<Attachment>(
            "SELECT * FROM attachments")).ToList();
        
        return (messages, attachments);
    }
}
```

---

## 📚 참고 자료

레포 안의 문서:
- [`README.md`](../README.md) — NOAH 프로젝트
- [`docs/v0.1_spec.md`](../docs/v0.1_spec.md) — v0.1 명세 (반드시 읽기)
- [`docs/Noah_design.md`](../docs/Noah_design.md) — 종합 설계 (필요시)
- [`server/`](../server/) — 서버 코드 (API 참고)
- [`server/routes/`](../server/routes/) — REST API 명세

비손서버 작업 진행 상황:
- [`channel/bison_server_message_001.md`](bison_server_message_001.md) — 서버 의뢰
- [`channel/crew_answer_001.md`](crew_answer_001.md) — 비손서버 질문 답변

---

## 🔄 작업 순서 (권장)

```
Day 1 (8h):
  Phase A 완료 (기초)
  Phase B 시작 (로그인 화면)

Day 2 (8h):
  Phase B 완료 (인증)
  Phase C 완료 (메인)

Day 3 (8h):
  Phase D 진행 (채팅방, 메시지)
  통합 테스트
  종료 조건 검증
```

---

## 💬 보고 양식

작업 진행 중 보고:
```bash
cd C:\WindowsApp\Noah
git add channel\
git commit -m "bison_pc: NOAH v0.1 진행 보고 #001"
git push
```

파일 양식:
```
channel/bison_pc_message_001.md  (진행 보고)
channel/bison_pc_question_001.md (질문)
```

---

## ⚠️ 주의사항

1. **NeoStock Phase 7 우선** — 안목과 함께 진행 중인 차트 작업이 우선
2. **서버 의존** — NOAH 서버가 동작해야 PC 클라이언트 테스트 가능
3. **WebSocket 안정성** — 자동 재연결 필수 (사용자가 매일 쓸 도구)
4. **사용자 데이터 통제** — 채팅방 닫을 때 반드시 저장 다이얼로그
5. **메모리 관리** — 30분 사용 시 크래시 0회 목표

---

## 🎯 작업 시작 전 체크리스트

```
[ ] git pull로 최신 NOAH 레포 받음
[ ] docs/v0.1_spec.md 읽음
[ ] 비손서버에서 NOAH 서버 외부 URL 받음 (또는 localhost:4001 테스트)
[ ] .NET 8 SDK 설치 확인
[ ] HandyControl NuGet 확인
[ ] NeoStock Phase 7 작업 상태 확인
```

---

## 🛶 방주에서 만나요

NOAH가 동작하기 시작하면 git push 대신 NOAH 메신저 자체로 통신할 예정입니다.

Phase 7 → NOAH v0.1 (서버 + PC + 모바일) 완성 → 모두 NOAH 채팅방으로 이동.

지금 git push 워크플로의 마지막 단계입니다.

화이팅!

---

## 📌 한 줄 요약

```
1. 위치: C:\WindowsApp\Noah\pc
2. 스택: .NET 8 WPF + HandyControl
3. 서버: 비손서버에게 받은 URL (포트 4001)
4. DB: system.db (본체) + .noahdb (대화 파일, 사용자 지정)
5. 핵심: 안정성 + 사용자 데이터 통제
6. 시간: 24h (3일)
```

---

*— agent_crew | 2026-04-13*  
*NOAH = Networked Operations Agent Hub*
