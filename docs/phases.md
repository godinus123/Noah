# NOAH — Phase 정의 및 로드맵

> **NOAH = Networked Operations Agent Hub**

작은 것부터 시작해서 점진적으로 확장합니다.  
각 Phase는 **동작하는 결과물**을 제공하며, Phase 종료 시 모두에게 시연 가능합니다.

---

## 🎯 Phase 철학

```
"작게 시작하고, 자주 동작 확인하고, 점진적으로 확장"

❌ 안 좋은 방식:
   3개월 동안 설계만 → 한 번에 모든 코드 작성 → 동작 안 함 → 멘붕

✅ 좋은 방식 (NOAH 방식):
   3일 미니멀 동작 → 1주 기본 기능 → 1주 고급 기능 → ...
   매 Phase마다 동작 확인
```

---

## 🟢 Phase 0 — 미니멀 (3일, ~12h)

### 목표
> **"Hello"가 두 디바이스 사이를 오가는 것**

이것만 됨. 다른 기능 X. 동작하면 그게 곧 성공.

### 범위

```
✅ WebSocket echo server (Node.js, ~50줄)
✅ WPF 미니멀 클라이언트 (입력창 + 메시지 리스트)
✅ MAUI 미니멀 클라이언트 (같은 거)
✅ 디바이스 ID UUID 자동 생성
✅ 같은 PC에서 두 클라 동시 실행 (다른 폴더)
✅ 메시지 송수신 동작

❌ DB 없음 (메모리만)
❌ 인증 없음
❌ 첨부 없음
❌ 푸시 없음
❌ 카카오톡 UI X (단순 리스트만)
```

### Phase 0 작업 분배

#### #P0-1 [server] WebSocket Echo Server
**담당**: 비손서버  
**시간**: 2h  
**산출물**: `server/server.js`

```javascript
// server/server.js (목표: ~50줄)
const WebSocket = require('ws');
const wss = new WebSocket.Server({ port: 3001 });

const clients = new Map(); // device_id -> ws

wss.on('connection', (ws) => {
  let deviceId = null;
  
  ws.on('message', (data) => {
    const msg = JSON.parse(data);
    
    if (msg.type === 'register') {
      deviceId = msg.device_id;
      clients.set(deviceId, ws);
      ws.send(JSON.stringify({ type: 'registered', device_id: deviceId }));
      
      // 모두에게 새 디바이스 알림
      broadcast({ type: 'device_joined', device_id: deviceId });
    }
    
    if (msg.type === 'message') {
      // 모든 연결된 클라이언트에 broadcast
      broadcast({
        type: 'message',
        from: deviceId,
        text: msg.text,
        timestamp: Date.now()
      });
    }
  });
  
  ws.on('close', () => {
    if (deviceId) {
      clients.delete(deviceId);
      broadcast({ type: 'device_left', device_id: deviceId });
    }
  });
});

function broadcast(msg) {
  const data = JSON.stringify(msg);
  clients.forEach((ws) => {
    if (ws.readyState === WebSocket.OPEN) ws.send(data);
  });
}

console.log('NOAH Phase 0 server running on :3001');
```

**검증**:
```bash
cd server
npm init -y
npm install ws
node server.js
# 다른 터미널에서 wscat -c ws://localhost:3001 으로 테스트
```

---

#### #P0-2 [pc] WPF 미니멀 클라이언트
**담당**: 비손피씨  
**시간**: 4h  
**산출물**: `pc/Noah/`

```
pc/Noah/
├── Noah.csproj
├── App.xaml
├── App.xaml.cs
├── MainWindow.xaml         ← 단일 화면
├── MainWindow.xaml.cs
└── Services/
    └── WebSocketClient.cs
```

```xml
<!-- MainWindow.xaml -->
<Window Title="NOAH" Width="600" Height="800">
  <Grid Margin="10">
    <Grid.RowDefinitions>
      <RowDefinition Height="Auto"/>
      <RowDefinition Height="*"/>
      <RowDefinition Height="Auto"/>
    </Grid.RowDefinitions>
    
    <!-- 헤더 -->
    <TextBlock Grid.Row="0" Text="🕊️ NOAH Phase 0" FontSize="20" Margin="0,0,0,10"/>
    
    <!-- 메시지 리스트 -->
    <ListBox Grid.Row="1" Name="MessagesList" Margin="0,0,0,10"/>
    
    <!-- 입력 -->
    <Grid Grid.Row="2">
      <Grid.ColumnDefinitions>
        <ColumnDefinition Width="*"/>
        <ColumnDefinition Width="Auto"/>
      </Grid.ColumnDefinitions>
      <TextBox Grid.Column="0" Name="InputBox" KeyDown="OnKeyDown"/>
      <Button Grid.Column="1" Content="전송" Click="OnSend" Margin="5,0,0,0"/>
    </Grid>
  </Grid>
</Window>
```

```csharp
// MainWindow.xaml.cs (~50줄)
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

public partial class MainWindow : Window
{
    private ClientWebSocket _ws;
    private string _deviceId;
    
    public MainWindow()
    {
        InitializeComponent();
        _deviceId = $"pc-{Guid.NewGuid().ToString().Substring(0, 8)}";
        Title = $"NOAH Phase 0 - {_deviceId}";
        _ = ConnectAsync();
    }
    
    private async Task ConnectAsync()
    {
        _ws = new ClientWebSocket();
        await _ws.ConnectAsync(new Uri("ws://localhost:3001"), default);
        
        // 등록
        await SendRawAsync(new { type = "register", device_id = _deviceId });
        
        // 수신 루프
        _ = Task.Run(ReceiveLoop);
    }
    
    private async Task ReceiveLoop()
    {
        var buffer = new byte[4096];
        while (_ws.State == WebSocketState.Open)
        {
            var result = await _ws.ReceiveAsync(buffer, default);
            var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
            var msg = JsonSerializer.Deserialize<Dictionary<string, object>>(json);
            
            Dispatcher.Invoke(() =>
            {
                MessagesList.Items.Add($"[{msg["from"]}]: {msg["text"]}");
            });
        }
    }
    
    private async void OnSend(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrEmpty(InputBox.Text)) return;
        
        await SendRawAsync(new { 
            type = "message", 
            text = InputBox.Text 
        });
        InputBox.Clear();
    }
    
    private void OnKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter) OnSend(sender, e);
    }
    
    private async Task SendRawAsync(object obj)
    {
        var json = JsonSerializer.Serialize(obj);
        var bytes = Encoding.UTF8.GetBytes(json);
        await _ws.SendAsync(bytes, WebSocketMessageType.Text, true, default);
    }
}
```

**검증**:
```powershell
cd pc/Noah
dotnet run
# 두 번 실행 (다른 디바이스 ID로 보임)
# 한쪽에서 텍스트 입력 → 다른쪽에 표시 확인
```

---

#### #P0-3 [mobile] MAUI 미니멀 클라이언트
**담당**: 안목  
**시간**: 4h  
**산출물**: `mobile/Noah/`

WPF와 거의 동일한 구조, MAUI XAML로 작성.

```xml
<!-- MainPage.xaml -->
<ContentPage Title="NOAH">
  <Grid Padding="10" RowDefinitions="Auto,*,Auto">
    <Label Grid.Row="0" Text="🕊️ NOAH Phase 0" FontSize="20"/>
    
    <CollectionView Grid.Row="1" x:Name="MessagesList"/>
    
    <Grid Grid.Row="2" ColumnDefinitions="*,Auto">
      <Entry Grid.Column="0" x:Name="InputBox" Keyboard="Chat" 
             Completed="OnSend"/>
      <Button Grid.Column="1" Text="전송" Clicked="OnSend"/>
    </Grid>
  </Grid>
</ContentPage>
```

```csharp
// MainPage.xaml.cs (WPF와 거의 동일, MAUI 컨트롤 사용)
public partial class MainPage : ContentPage
{
    private ClientWebSocket _ws;
    private string _deviceId;
    private ObservableCollection<string> _messages = new();
    
    public MainPage()
    {
        InitializeComponent();
        MessagesList.ItemsSource = _messages;
        _deviceId = $"mobile-{Guid.NewGuid().ToString().Substring(0, 8)}";
        _ = ConnectAsync();
    }
    
    private async Task ConnectAsync()
    {
        _ws = new ClientWebSocket();
        // 안드로이드 에뮬레이터에서 호스트 PC: 10.0.2.2
        // 실기기에서 같은 WiFi: 192.168.x.x
        await _ws.ConnectAsync(new Uri("ws://10.0.2.2:3001"), default);
        
        await SendRawAsync(new { type = "register", device_id = _deviceId });
        _ = Task.Run(ReceiveLoop);
    }
    
    // ... (WPF와 동일)
}
```

**검증**:
```bash
cd mobile/Noah
dotnet build -f net9.0-android
adb install bin/Debug/...
# 폰에서 실행 → PC 클라와 메시지 교환
```

---

#### #P0-4 [docs] Phase 0 빌드/실행 안내
**담당**: 크루  
**시간**: 2h  
**산출물**: `docs/phase0_quickstart.md`

각 컴포넌트 빌드/실행 방법, 트러블슈팅, 검증 시나리오.

---

### Phase 0 종료 조건

```
[ ] 비손서버: WebSocket 서버 동작
[ ] 비손피씨: WPF 클라 빌드 + 실행 + 메시지 전송
[ ] 안목: MAUI 클라 빌드 + 폰에 설치 + 메시지 전송
[ ] PC ↔ PC: 메시지 교환 (같은 PC 두 인스턴스)
[ ] PC ↔ 모바일: 메시지 교환 (다른 디바이스)
[ ] 시연 영상 (또는 캡처)
```

7개 모두 OK → Phase 0 종료, Phase 1A 시작.

---

## 🟡 Phase 1A — 기본 메신저 (1주, ~28h)

### 목표
> **"제대로 된 메신저처럼 동작"**

### 추가 기능
```
✅ SQLite (메시지 영구 저장)
✅ 디바이스 등록 + 인증
✅ 채팅방 개념 (1:1)
✅ 메시지 큐 (오프라인 사용자)
✅ FCM 푸시 (모바일)
✅ Toast 알림 (PC)
✅ 첨부 파일 업로드/다운로드
✅ 카카오톡 스타일 UI (Syncfusion / HandyControl)
✅ 빌드 분리 (Noah_BisonPC / Noah_Anmok / Noah_Crew)
✅ 디바이스 ID 영구 저장
```

### 작업 (Issues)
- #1A-1 [server] SQLite 스키마 + 디바이스 등록
- #1A-2 [server] 메시지 큐 + ACK
- #1A-3 [server] 첨부 파일 업로드/다운로드
- #1A-4 [server] FCM 푸시
- #1A-5 [pc] 채팅방 목록 UI (HandyControl)
- #1A-6 [pc] 채팅방 메시지 UI
- #1A-7 [pc] SQLite 로컬 + 동기화
- #1A-8 [pc] 첨부 드래그앤드롭 + 클립보드
- #1A-9 [pc] Toast 알림 + 작업표시줄 배지
- #1A-10 [pc] 빌드 분리 (AssemblyName)
- #1A-11 [mobile] 채팅방 UI (Syncfusion SfChat)
- #1A-12 [mobile] FCM 수신 + 알림
- #1A-13 [mobile] 카메라/갤러리 첨부
- #1A-14 [mobile] 배지 (ShortcutBadger)
- #1A-15 [docs] Phase 1A 사용자 가이드

---

## 🟠 Phase 1B — 고급 기능 (1주, ~35h)

### 목표
> **"카카오톡 + AI 협업"**

### 추가 기능
```
✅ WebRTC P2P (1:1 직접 통신)
✅ 그룹 채팅
✅ 답장 인용
✅ 이모지 반응
✅ HTML 메시지 (sanitized)
✅ PDF 자체 뷰어 (PDFium)
✅ 이미지 풀스크린 뷰어
✅ 비디오 썸네일 + OS 위임 재생
✅ 그림판 메시지 (SkiaSharp)
✅ AI 에이전트 통합 (사람 + AI 혼합 채팅)
✅ AI SDK (Python, Node.js)
```

### 작업
- #1B-1 [server] WebRTC 시그널링
- #1B-2 [server] 그룹 채팅 라우팅
- #1B-3 [server] AI 토큰 발급
- #1B-4 [pc] WebRTC 클라이언트 (SipSorcery)
- #1B-5 [mobile] WebRTC 클라이언트
- #1B-6 [pc] PDF 뷰어 (PdfiumViewer)
- #1B-7 [mobile] PDF 뷰어 (PdfiumViewer.Maui)
- #1B-8 [all] HTML 메시지 sanitize
- #1B-9 [all] 답장 인용 UI
- #1B-10 [all] 이모지 반응 UI
- #1B-11 [all] 그림판 (SkiaSharp)
- #1B-12 [all] 비디오 썸네일
- #1B-13 [ai_sdk] Python 클라이언트
- #1B-14 [ai_sdk] Node.js 클라이언트
- #1B-15 [docs] AI 에이전트 통합 가이드

---

## 🔵 Phase 2 — 풀 기능 (1주, ~40h)

### 목표
> **"오픈소스 메신저로서의 완성도"**

### 추가 기능
```
✅ P2P DB sync (디바이스 간 히스토리 복구)
✅ DOCX/XLSX/PPTX 뷰어 (OpenXML)
✅ 음성 메시지 (녹음/재생)
✅ 다크 모드
✅ 다국어 (한/영)
✅ 자동화 테스트 (CI/CD)
✅ 부하 테스트 (Locust)
✅ 보안 테스트
✅ 사용자 가이드 (스크린샷 포함)
```

### 작업
- #2-1 [all] P2P DB sync 프로토콜
- #2-2 [all] DOCX 뷰어
- #2-3 [all] XLSX 뷰어
- #2-4 [all] PPTX 뷰어
- #2-5 [all] 음성 녹음/재생
- #2-6 [all] 다크 모드
- #2-7 [all] 영어 번역
- #2-8 [server] 부하 테스트 환경
- #2-9 [tests] 자동화 테스트
- #2-10 [docs] 사용자 가이드

---

## 🟣 Phase 3 — 보안/확장 (선택)

### 목표
> **"엔터프라이즈 수준 보안"**

```
- E2E 암호화 (Signal Protocol)
- 디바이스별 키쌍
- SMS 전화번호 인증
- 카카오 로그인 (선택)
- 자체 도메인 + Let's Encrypt
- pm2 클러스터 모드
- 메트릭 (Prometheus)
- 로그 (ELK)
```

---

## 📊 진행 상황 추적

GitHub Project Board (Kanban):

```
| Backlog | Phase 0 | In Progress | Review | Done |
|---------|---------|-------------|--------|------|
| #1B-1   | #P0-1   | #P0-2       | #P0-3  |      |
| #1B-2   | #P0-4   |             |        |      |
| ...     |         |             |        |      |
```

각 issue가 카드로 표시. 작업 진행에 따라 이동.

---

## 🎯 일정 (가정)

```
Week 1: Phase 0 + Phase 1A 시작
Week 2: Phase 1A 완료 + Phase 1B 시작
Week 3: Phase 1B 완료 + Phase 2 시작
Week 4: Phase 2 완료 + 안정화
Week 5+: Phase 3 (선택)
```

총 약 1개월. 4명 병렬 작업 기준.

---

## 🚀 시작 명령

```bash
# 처음 시작하시는 분
git clone https://github.com/godinus123/Noah.git
cd Noah
cat README.md             # 프로젝트 소개
cat CONTRIBUTING.md       # 기여 방법
cat docs/phases.md        # 이 문서 (Phase 정의)
cat docs/Noah_design.md   # 종합 설계 (1300+줄)

# Issue 확인
gh issue list --label "phase-0"

# 작업 시작
git checkout -b feature/your-task
```

---

*— agent_crew | 2026-04-13*
