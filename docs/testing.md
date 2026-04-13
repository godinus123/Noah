# Noah — 테스트 & 시뮬레이션 가이드

**작성자**: agent_crew
**문서 목적**: 코딩 + 테스트 시 문제점 시뮬레이션, 테스트 방법, 5대 디바이스 시뮬

---

## 1. 테스트 전략

### 1.1 테스트 피라미드

```
        ┌────────┐
        │수동 E2E │  ← 적게 (사용자 시나리오)
        ├────────┤
        │ 통합테스트│  ← 중간 (컴포넌트 간)
        ├────────┤
        │ 단위테스트│  ← 많이 (함수)
        └────────┘
```

### 1.2 도구

| 단계 | 모바일 | PC | 서버 |
|------|--------|-----|------|
| 단위 | xUnit | xUnit | Jest |
| 통합 | xUnit + TestDevice | xUnit + TestDevice | Jest + supertest |
| E2E | Appium | WinAppDriver | Playwright |
| 부하 | - | - | Locust / k6 |
| 보안 | - | - | OWASP ZAP, sqlmap |

---

## 2. 시뮬레이션 시나리오 12가지

### S1. 메시지 폭주
```
시나리오: 5명이 동시에 메시지 100개씩 전송 (총 500개)
검증:
  - 메시지 순서 유지 (server_seq)
  - 중복 표시 X
  - 클라 메모리 폭발 X
  - UI 멈춤 X
  - 모든 디바이스 같은 순서로 표시
```

### S2. 네트워크 단절/복구
```
시나리오: WebRTC 연결 중 와이파이 끊김 → 5초 후 복구
검증:
  - 메시지 손실 X
  - 자동 재연결
  - 큐로 자동 폴백
  - 사용자 알림 표시 ("연결 끊김" → "재연결됨")
```

### S3. 디바이스 동시성
```
시나리오: 같은 사용자의 폰+PC+태블릿 3대 동시 사용
  - 폰에서 메시지 전송
  - PC에서 같은 메시지 받기 (자기 자신 동기화)
  - 태블릿에서 답장 전송
검증:
  - 메시지 ID 중복 X
  - server_seq 정렬 정확
  - 모든 디바이스에 같은 내용 표시
```

### S4. 메모리 누수
```
시나리오: 30분 동안 채팅방 50회 전환 + 메시지 1000개 송수신
검증:
  - 메모리 그래프 우상향 X
  - GC 정상 동작
  - 누수 추적 (dotMemory, Android Profiler)
도구:
  - 모바일: Android Studio Profiler
  - PC: dotnet-counters, dotMemory
  - 서버: clinic.js, node --inspect
```

### S5. 큰 파일 첨부
```
시나리오: 100MB PDF 전송, 받는 디바이스 5대 (3 온라인, 2 오프라인)
검증:
  - 업로드 진행률 표시
  - P2P vs 서버 분기 (1MB 기준)
  - 청크 분할 (16KB)
  - 만료 정리 (7일 후)
  - 받는 쪽 메모리 폭발 X
```

### S6. 악의적 입력
```
시나리오: 위험한 메시지/입력 시도
  - HTML XSS: <script>alert(1)</script>
  - 매우 긴 텍스트 (10MB)
  - 특수 유니코드 (RTL, 제로폭, 대량 이모지)
  - SQL injection: ' OR '1'='1
  - 잘못된 JSON
  - 거대 파일명 (1000자)
검증: 모두 안전하게 처리, 앱/서버 크래시 X
```

### S7. 동시 가입
```
시나리오: 100명이 동시에 회원가입 + 디바이스 등록
검증:
  - 모두 성공
  - 디바이스 ID 중복 X
  - DB 트랜잭션 무결성
```

### S8. 그룹 채팅 부하
```
시나리오: 100명 그룹 채팅방 → 1명이 메시지 전송
검증:
  - 99명 모두에게 배달 (push)
  - 99개 큐 row 동시 생성
  - server_seq 단일
  - ACK 99개 처리 시간 < 5초
```

### S9. WebRTC NAT 통과
```
시나리오: 다른 네트워크의 두 디바이스 (서로 다른 NAT)
  - 한쪽이 회사 방화벽 안
검증:
  - STUN 서버로 통과 시도
  - 실패 시 TURN 또는 서버 폴백
  - 메시지 손실 X
```

### S10. FCM 푸시 안정성
```
시나리오: 오프라인 디바이스 100개에 메시지 동시 전송
검증:
  - FCM 모두 호출 (batch 처리)
  - 도착률 (FCM은 99% 이상)
  - 푸시 알림 탭 → 정확한 채팅방 진입
  - 읽음 처리
```

### S11. 큐 만료
```
시나리오: 메시지 큐에 1주일 이상 대기 중인 메시지
검증:
  - 7일 후 cron이 자동 삭제
  - 디스크 공간 확보
  - 만료 메시지 받는 쪽에 "미배달" 표시
```

### S12. 디바이스 재연결
```
시나리오: 일주일 동안 꺼져 있던 디바이스 재시작
검증:
  - 마지막 server_seq 이후 메시지 모두 받기
  - 만료된 메시지는 못 받음 (정상)
  - 채팅방 unread_count 정확
  - 동기화 진행률 표시
```

---

## 3. 5대 디바이스 시뮬레이션 환경

가짜 디바이스 5대 동시 시뮬. 실제 폰 없이도 그룹 채팅 부하 테스트.

### 3.1 TestDevice 클래스 (C#)

```csharp
public class TestDevice : IDisposable
{
    public string Id { get; }
    public string Name { get; }
    public List<Message> Received { get; } = new();
    public bool IsOnline { get; private set; }
    
    private ClientWebSocket _ws;
    private CancellationTokenSource _cts = new();
    
    public TestDevice(string name)
    {
        Name = name;
        Id = $"test_{name}_{Guid.NewGuid():N}".Substring(0, 24);
        _ws = new ClientWebSocket();
    }
    
    public async Task ConnectAsync(string serverUrl = "ws://localhost:3001/ws")
    {
        await _ws.ConnectAsync(new Uri(serverUrl), _cts.Token);
        IsOnline = true;
        
        // 인증
        await SendRawAsync(new {
            type = "auth",
            device_id = Id,
            token = "test-token"
        });
        
        // 수신 루프
        _ = Task.Run(ReceiveLoopAsync);
    }
    
    public async Task SendMessage(string text, string targetDeviceId)
    {
        var msg = new {
            type = "message",
            msg_id = Guid.NewGuid().ToString(),
            from_device_id = Id,
            target_device_id = targetDeviceId,
            text = text,
            timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };
        await SendRawAsync(msg);
    }
    
    public async Task SendFile(string targetDeviceId, byte[] fileData, string filename)
    {
        // 1MB 이하 → P2P, 초과 → 서버
        var msg = new {
            type = "file",
            msg_id = Guid.NewGuid().ToString(),
            target_device_id = targetDeviceId,
            filename = filename,
            data = Convert.ToBase64String(fileData)
        };
        await SendRawAsync(msg);
    }
    
    public async Task<Message> WaitForMessageAsync(TimeSpan timeout)
    {
        var sw = Stopwatch.StartNew();
        var initialCount = Received.Count;
        
        while (sw.Elapsed < timeout)
        {
            if (Received.Count > initialCount)
                return Received[initialCount];
            await Task.Delay(50);
        }
        throw new TimeoutException($"{Name}이 메시지 대기 중 타임아웃");
    }
    
    private async Task ReceiveLoopAsync()
    {
        var buffer = new byte[1024 * 64];
        while (!_cts.IsCancellationRequested)
        {
            try
            {
                var result = await _ws.ReceiveAsync(buffer, _cts.Token);
                if (result.MessageType == WebSocketMessageType.Text)
                {
                    var json = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    var msg = JsonSerializer.Deserialize<Message>(json);
                    if (msg != null) Received.Add(msg);
                }
            }
            catch { break; }
        }
    }
    
    private async Task SendRawAsync(object obj)
    {
        var json = JsonSerializer.Serialize(obj);
        var bytes = Encoding.UTF8.GetBytes(json);
        await _ws.SendAsync(bytes, WebSocketMessageType.Text, true, _cts.Token);
    }
    
    public async Task GoOfflineAsync()
    {
        IsOnline = false;
        await _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "test offline", _cts.Token);
    }
    
    public void Dispose()
    {
        _cts.Cancel();
        _ws?.Dispose();
    }
}
```

### 3.2 5대 디바이스 셋업

```csharp
public class FiveDeviceSimulation
{
    private List<TestDevice> _devices = new();
    
    public async Task SetupAsync()
    {
        // 5대 동시 생성 + 연결
        var names = new[] { "Phone1", "Phone2", "PC1", "Tablet", "Web" };
        var tasks = names.Select(async name =>
        {
            var d = new TestDevice(name);
            await d.ConnectAsync();
            return d;
        });
        _devices = (await Task.WhenAll(tasks)).ToList();
        
        Console.WriteLine($"5대 디바이스 연결 완료:");
        foreach (var d in _devices)
            Console.WriteLine($"  {d.Name}: {d.Id}");
    }
    
    public TestDevice this[int index] => _devices[index];
    public TestDevice this[string name] => _devices.First(d => d.Name == name);
    
    public async Task TearDownAsync()
    {
        foreach (var d in _devices)
        {
            await d.GoOfflineAsync();
            d.Dispose();
        }
    }
}
```

### 3.3 시나리오 1: 메시지 broadcast

```csharp
[Fact]
public async Task FiveDevices_OneSendsToAll()
{
    var sim = new FiveDeviceSimulation();
    await sim.SetupAsync();
    
    try
    {
        // Phone1이 나머지 4대에게 메시지 전송
        var sender = sim["Phone1"];
        var receivers = new[] { "Phone2", "PC1", "Tablet", "Web" };
        
        foreach (var name in receivers)
            await sender.SendMessage("hello all", sim[name].Id);
        
        // 4대 모두 받았는지 확인
        var tasks = receivers.Select(name => sim[name].WaitForMessageAsync(TimeSpan.FromSeconds(5)));
        var results = await Task.WhenAll(tasks);
        
        foreach (var msg in results)
            Assert.Equal("hello all", msg.Text);
    }
    finally
    {
        await sim.TearDownAsync();
    }
}
```

### 3.4 시나리오 2: 그룹 채팅 (5명 동시 전송)

```csharp
[Fact]
public async Task FiveDevices_AllSendSimultaneously()
{
    var sim = new FiveDeviceSimulation();
    await sim.SetupAsync();
    
    var roomId = "test-room-1";
    
    // 5대 모두 동시에 메시지 10개씩 전송 (총 50개)
    var sendTasks = Enumerable.Range(0, 5).Select(async i =>
    {
        var device = sim[i];
        for (int j = 0; j < 10; j++)
        {
            await device.SendMessage($"{device.Name}-msg-{j}", roomId);
            await Task.Delay(50);
        }
    });
    await Task.WhenAll(sendTasks);
    
    // 모든 디바이스가 50개씩 받는지 확인 (자기 자신 메시지 제외 = 40개)
    await Task.Delay(2000); // sync 대기
    
    foreach (var d in Enumerable.Range(0, 5).Select(i => sim[i]))
    {
        var ownMsgs = d.Received.Count(m => m.FromDeviceId == d.Id);
        var otherMsgs = d.Received.Count(m => m.FromDeviceId != d.Id);
        
        Console.WriteLine($"{d.Name}: 자기={ownMsgs}, 타인={otherMsgs}");
        Assert.Equal(40, otherMsgs); // 4명 × 10개
    }
    
    await sim.TearDownAsync();
}
```

### 3.5 시나리오 3: 오프라인 → 온라인 동기화

```csharp
[Fact]
public async Task DeviceOffline_ReceivesOnReconnect()
{
    var sim = new FiveDeviceSimulation();
    await sim.SetupAsync();
    
    // Phone2를 오프라인으로
    await sim["Phone2"].GoOfflineAsync();
    
    // 다른 디바이스들이 메시지 전송
    await sim["Phone1"].SendMessage("offline test 1", sim["Phone2"].Id);
    await sim["PC1"].SendMessage("offline test 2", sim["Phone2"].Id);
    await sim["Tablet"].SendMessage("offline test 3", sim["Phone2"].Id);
    
    await Task.Delay(1000);
    
    // 서버 큐에 3개 메시지 있어야 함
    // (서버 API로 확인)
    
    // Phone2 다시 온라인
    var phone2Reconnect = new TestDevice("Phone2-reconnect");
    await phone2Reconnect.ConnectAsync();
    
    // 큐에 있던 3개 메시지 받기
    var msgs = new List<Message>();
    for (int i = 0; i < 3; i++)
    {
        msgs.Add(await phone2Reconnect.WaitForMessageAsync(TimeSpan.FromSeconds(5)));
    }
    
    Assert.Equal(3, msgs.Count);
    
    await sim.TearDownAsync();
}
```

### 3.6 시나리오 4: 첨부 파일 전송

```csharp
[Fact]
public async Task FiveDevices_LargeFileBroadcast()
{
    var sim = new FiveDeviceSimulation();
    await sim.SetupAsync();
    
    // 5MB 파일 생성
    var fileData = new byte[5 * 1024 * 1024];
    new Random().NextBytes(fileData);
    
    // Phone1이 파일 전송
    foreach (var name in new[] { "Phone2", "PC1", "Tablet", "Web" })
    {
        await sim["Phone1"].SendFile(sim[name].Id, fileData, "test.bin");
    }
    
    // 4대 모두 받기
    foreach (var name in new[] { "Phone2", "PC1", "Tablet", "Web" })
    {
        var msg = await sim[name].WaitForMessageAsync(TimeSpan.FromSeconds(30));
        Assert.Equal("test.bin", msg.Filename);
        Assert.Equal(fileData.Length, msg.FileSize);
    }
    
    await sim.TearDownAsync();
}
```

### 3.7 시나리오 5: 메시지 순서 검증

```csharp
[Fact]
public async Task FiveDevices_MessageOrder_Consistent()
{
    var sim = new FiveDeviceSimulation();
    await sim.SetupAsync();
    
    var roomId = "order-test";
    
    // Phone1이 100개 메시지 빠르게 전송
    for (int i = 0; i < 100; i++)
    {
        await sim["Phone1"].SendMessage($"order-{i:D3}", roomId);
    }
    
    await Task.Delay(3000);
    
    // 모든 디바이스가 같은 순서로 받았는지 확인
    var phone2Order = sim["Phone2"].Received.Select(m => m.Text).ToList();
    var pc1Order = sim["PC1"].Received.Select(m => m.Text).ToList();
    
    Assert.Equal(100, phone2Order.Count);
    Assert.Equal(phone2Order, pc1Order); // 같은 순서
    
    // 순서 확인
    for (int i = 0; i < 100; i++)
    {
        Assert.Equal($"order-{i:D3}", phone2Order[i]);
    }
    
    await sim.TearDownAsync();
}
```

---

## 4. 부하 테스트 (Locust)

### 4.1 1000명 동접 시뮬

```python
# load_test.py
from locust import HttpUser, task, between
import json

class NoahUser(HttpUser):
    wait_time = between(0.5, 2)
    
    def on_start(self):
        # 가짜 디바이스 등록
        resp = self.client.post("/api/devices/register", json={
            "device_name": f"loadtest_{self.environment.runner.user_count}",
            "device_type": "test"
        })
        self.device_id = resp.json()["device_id"]
        self.token = resp.json()["token"]
    
    @task(3)
    def send_message(self):
        self.client.post("/api/messages", json={
            "msg_id": str(uuid.uuid4()),
            "room_id": "load-room",
            "text": "load test message",
            "client_timestamp": int(time.time() * 1000)
        }, headers={"Authorization": f"Bearer {self.token}"})
    
    @task(1)
    def get_pending(self):
        self.client.get("/api/messages/pending", 
                        headers={"Device-Id": self.device_id})
```

```bash
# 실행
locust -f load_test.py --host=http://localhost:3001 -u 1000 -r 100

# 결과 (대시보드: http://localhost:8089)
- 1000 users
- 100 spawn rate
- 메시지 송신: ~3000 req/sec
- p95 응답시간: < 200ms
- 실패율: < 0.1%
```

### 4.2 목표 성능

```
- 메시지 송수신:    1000 req/sec, p95 < 200ms
- WebSocket 동접:   10,000 connections
- DB 쓰기:          5,000 ops/sec
- 첨부 업로드:      100 MB/sec
- 메모리 (서버):    < 1 GB
- CPU (서버):       < 70%
```

이 수치 못 맞추면 → 서버 분산, Redis 캐시, DB 최적화 필요.

---

## 5. 보안 테스트

### 5.1 XSS 페이로드 100개

```python
# xss_test.py
xss_payloads = [
    '<script>alert(1)</script>',
    '<img src=x onerror=alert(1)>',
    '<svg onload=alert(1)>',
    'javascript:alert(1)',
    '"><script>alert(1)</script>',
    '<iframe src=javascript:alert(1)>',
    # ... 100개
]

for payload in xss_payloads:
    resp = requests.post("http://localhost:3001/api/messages", json={
        "type": "html",
        "content": payload
    })
    
    # 받는 쪽에서 sanitize 되었는지 확인
    received = get_message(resp.json()["msg_id"])
    assert "<script" not in received["content"]
    assert "javascript:" not in received["content"]
    assert "onerror" not in received["content"]
```

### 5.2 SQL Injection

```bash
# sqlmap 자동
sqlmap -u "http://localhost:3001/api/messages?room_id=test" \
       --headers="Authorization: Bearer test" \
       --batch \
       --level=5 --risk=3
```

### 5.3 인증 우회

```python
# 토큰 없이 API 호출
resp = requests.get("http://localhost:3001/api/messages")
assert resp.status_code == 401  # Unauthorized

# 위조 토큰
resp = requests.get("http://localhost:3001/api/messages",
                   headers={"Authorization": "Bearer fake"})
assert resp.status_code == 401

# 다른 사용자 디바이스 ID
resp = requests.get("http://localhost:3001/api/messages/pending",
                   headers={"Device-Id": "other-user-device"})
assert resp.status_code == 403  # Forbidden
```

---

## 6. 수동 E2E 체크리스트

### Phase 1A 종료 조건

```
[ ] T1.  앱 시작 → 디바이스 자동 등록 (UUID 영구)
[ ] T2.  채팅방 생성 → 1:1 채팅
[ ] T3.  텍스트 메시지 송수신
[ ] T4.  이미지 첨부 → 썸네일 표시 → 원본 다운로드
[ ] T5.  PDF 첨부 → PDFium 뷰어로 열기 → 텍스트 복사
[ ] T6.  오프라인 → 메시지 작성 → 큐 저장 → 온라인 → 자동 전송
[ ] T7.  FCM 푸시 → 알림 표시 → 탭 → 채팅방 진입
[ ] T8.  같은 PC 두 인스턴스 (BisonPC + Anmok) → 메시지 교환
[ ] T9.  30분 사용 → 크래시 0회
[ ] T10. 메모리 사용량 < 200MB
```

### Phase 1B 종료 조건

```
[ ] T11. 그룹 채팅 (5명) → 동시 메시지 → 모두 같은 순서
[ ] T12. AI 에이전트 + 사람 혼합 채팅
[ ] T13. HTML 메시지 → sanitize 동작 → XSS 차단
[ ] T14. 답장 인용 → 원본 메시지 표시
[ ] T15. 이모지 반응 → 카운터 업데이트
[ ] T16. 음성 메시지 → 녹음 → 전송 → 재생
```

### Phase 2 종료 조건

```
[ ] T17. P2P DB sync → 새 디바이스 → 기존 히스토리 가져오기
[ ] T18. DOCX/XLSX/PPTX 뷰어
[ ] T19. 다크 모드
[ ] T20. 부하 테스트 1000명 동접 통과
```

---

## 7. CI/CD 자동화

### 7.1 GitHub Actions

```yaml
# .github/workflows/test.yml
name: Noah Test

on: [push, pull_request]

jobs:
  unit-tests:
    runs-on: ubuntu-latest
    steps:
      - uses: actions/checkout@v3
      - uses: actions/setup-node@v3
        with: { node-version: '20' }
      - run: cd server && npm ci && npm test
      
      - uses: actions/setup-dotnet@v3
        with: { dotnet-version: '8.0.x' }
      - run: cd pc && dotnet test
  
  integration-tests:
    runs-on: ubuntu-latest
    services:
      noah-server:
        image: noah/server:test
        ports: [3001:3001]
    steps:
      - run: cd tests && dotnet test --filter "Category=Integration"
  
  load-test:
    runs-on: ubuntu-latest
    needs: integration-tests
    steps:
      - run: |
          pip install locust
          locust -f tests/load_test.py --headless -u 100 -r 10 -t 60s
```

### 7.2 수동 테스트 트리거

```yaml
- name: Manual E2E Test Trigger
  uses: peter-evans/create-issue-from-file@v4
  with:
    title: "E2E Test Cycle - ${{ github.sha }}"
    content-filepath: docs/testing.md
    labels: testing, manual
```

---

## 8. 디버깅 도구

### 8.1 클라이언트 로그

```csharp
// MAUI/WPF 공통
public static class NoahLog
{
    private static StreamWriter _writer;
    
    public static void Initialize()
    {
        var path = Path.Combine(AppInfo.DataPath, "noah.log");
        _writer = new StreamWriter(path, append: true);
    }
    
    public static void D(string tag, string msg)
    {
        var line = $"{DateTime.Now:HH:mm:ss.fff} [D] {tag}: {msg}";
        _writer.WriteLine(line);
        _writer.Flush();
        System.Diagnostics.Debug.WriteLine(line);
    }
    
    public static void E(string tag, string msg, Exception ex = null)
    {
        var line = $"{DateTime.Now:HH:mm:ss.fff} [E] {tag}: {msg}";
        if (ex != null) line += $"\n{ex}";
        _writer.WriteLine(line);
        _writer.Flush();
    }
}
```

### 8.2 사용자가 로그 보내기

```csharp
[RelayCommand]
private async Task SendDiagnosticReport()
{
    var logFile = Path.Combine(AppInfo.DataPath, "noah.log");
    var report = new {
        device = AppInfo.DeviceName,
        version = AppInfo.Version,
        os = DeviceInfo.Platform.ToString(),
        log = await File.ReadAllTextAsync(logFile)
    };
    
    // 크루 (또는 개발자) 채팅방으로 자동 전송
    await ChatService.SendToCrew(report);
}
```

### 8.3 서버 로그 (winston)

```javascript
// server/logger.js
const winston = require('winston');

module.exports = winston.createLogger({
  level: 'info',
  format: winston.format.combine(
    winston.format.timestamp(),
    winston.format.json()
  ),
  transports: [
    new winston.transports.File({ filename: 'logs/error.log', level: 'error' }),
    new winston.transports.File({ filename: 'logs/combined.log' }),
    new winston.transports.Console({ format: winston.format.simple() })
  ]
});

// 사용
logger.info('Message sent', { from: 'A', to: 'B', size: 100 });
logger.error('DB error', { error: err.message });
```

---

## 9. 성능 모니터링

### 9.1 클라이언트

```csharp
// 메시지 전송 시간 측정
var sw = Stopwatch.StartNew();
await ChatService.SendMessage(text);
sw.Stop();
NoahLog.D("Perf", $"SendMessage: {sw.ElapsedMilliseconds}ms");

// 메모리 측정
var memBefore = GC.GetTotalMemory(false);
// ... 작업 ...
var memAfter = GC.GetTotalMemory(false);
NoahLog.D("Perf", $"Memory: +{(memAfter - memBefore) / 1024} KB");
```

### 9.2 서버

```javascript
// /metrics 엔드포인트 (Prometheus 호환)
app.get('/metrics', (req, res) => {
  res.json({
    active_connections: wsServer.clients.size,
    pending_messages: db.prepare('SELECT COUNT(*) FROM pending_messages').get(),
    pending_files_mb: getFilesSize() / 1024 / 1024,
    uptime: process.uptime(),
    memory_mb: process.memoryUsage().heapUsed / 1024 / 1024
  });
});
```

---

## 10. 트러블슈팅 가이드

### 자주 발생하는 문제

| 문제 | 원인 | 해결 |
|------|------|------|
| 메시지 도착 안 함 | WebSocket 끊김 | 재연결 로직, FCM 폴백 |
| 메시지 중복 표시 | msg_id 중복 처리 X | INSERT OR IGNORE |
| 메모리 누수 | 이벤트 핸들러 미해제 | Dispose 패턴 |
| PDF 안 열림 | PDFium native 라이브러리 누락 | 빌드 시 자동 복사 확인 |
| 푸시 안 옴 | FCM 토큰 만료 | 주기적 갱신 |
| 동기화 누락 | server_seq 충돌 | 트랜잭션 + UNIQUE 제약 |
| 빌드 실패 | 캐시 문제 | bin/obj 삭제 + 재빌드 |
| 데이터 유실 | 디바이스 ID 변경 | UUID 영구 저장 검증 |

---

*— agent_crew | 2026-04-13*
