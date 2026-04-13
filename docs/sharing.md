# Noah — 공유 (Share) 가이드

**작성자**: agent_crew
**문서 목적**: Windows + 모바일에서 다른 앱과의 공유 기능

---

## 1. 개요

Noah는 단순한 메신저가 아닙니다.  
**시스템 차원의 공유 허브** 역할을 합니다.

```
다른 앱 ──→ Noah ──→ 다른 사람/AI
   ↑                      ↓
   └───── Noah ──────────┘
```

### 1.1 양방향 공유

| 방향 | 의미 | 사용 예 |
|------|------|---------|
| **외부 → Noah** (받기) | 다른 앱에서 Noah로 보내기 | 갤러리 사진 → Noah로 공유 |
| **Noah → 외부** (보내기) | Noah에서 다른 앱으로 | Noah 메시지 → 카톡으로 공유 |

---

## 2. Windows (PC)

### 2.1 받기 (다른 앱 → Noah)

#### 방법 A: 드래그 앤 드롭
```csharp
// MainWindow.xaml
<Window AllowDrop="True" 
        DragEnter="OnDragEnter" 
        Drop="OnDrop">
```

```csharp
// MainWindow.xaml.cs
private void OnDragEnter(object sender, DragEventArgs e)
{
    if (e.Data.GetDataPresent(DataFormats.FileDrop) ||
        e.Data.GetDataPresent(DataFormats.Bitmap) ||
        e.Data.GetDataPresent(DataFormats.UnicodeText))
    {
        e.Effects = DragDropEffects.Copy;
    }
}

private async void OnDrop(object sender, DragEventArgs e)
{
    // 파일 드롭
    if (e.Data.GetDataPresent(DataFormats.FileDrop))
    {
        var files = (string[])e.Data.GetData(DataFormats.FileDrop);
        foreach (var file in files)
        {
            await ChatService.SendFile(file);
        }
    }
    // 이미지 드롭 (스크린샷 등)
    else if (e.Data.GetDataPresent(DataFormats.Bitmap))
    {
        var bitmap = (BitmapSource)e.Data.GetData(DataFormats.Bitmap);
        var path = SaveTempImage(bitmap);
        await ChatService.SendFile(path);
    }
    // 텍스트 드롭
    else if (e.Data.GetDataPresent(DataFormats.UnicodeText))
    {
        var text = (string)e.Data.GetData(DataFormats.UnicodeText);
        ChatViewModel.InputText = text;
    }
}
```

**사용**: 파일 탐색기에서 PDF 끌어다 Noah 창에 떨어뜨림 → 자동 첨부.

#### 방법 B: 클립보드
```
Ctrl+C로 복사한 것 → Noah 입력바에 Ctrl+V

지원:
- 텍스트
- 이미지 (캡처도구, 브라우저 이미지)
- 파일 경로
```

```csharp
private void InputBox_PreviewKeyDown(object sender, KeyEventArgs e)
{
    if (e.Key == Key.V && Keyboard.Modifiers == ModifierKeys.Control)
    {
        // 클립보드에 이미지가 있으면 자동 첨부
        if (Clipboard.ContainsImage())
        {
            var img = Clipboard.GetImage();
            var path = SaveTempImage(img);
            _ = ChatService.SendFile(path);
            e.Handled = true;
        }
        else if (Clipboard.ContainsFileDropList())
        {
            var files = Clipboard.GetFileDropList();
            foreach (string file in files)
            {
                _ = ChatService.SendFile(file);
            }
            e.Handled = true;
        }
    }
}
```

#### 방법 C: Windows 컨텍스트 메뉴 ("보내기 → Noah")

레지스트리 등록:

```reg
Windows Registry Editor Version 5.00

[HKEY_CLASSES_ROOT\*\shell\NoahSendTo]
@="Noah로 보내기"
"Icon"="C:\\Tools\\Noah\\Noah_BisonPC.exe,0"

[HKEY_CLASSES_ROOT\*\shell\NoahSendTo\command]
@="\"C:\\Tools\\Noah\\Noah_BisonPC.exe\" --send \"%1\""
```

이러면 어떤 파일이든 우클릭 → "Noah로 보내기" 표시.

```csharp
// App.xaml.cs - 시작 인자 처리
protected override void OnStartup(StartupEventArgs e)
{
    base.OnStartup(e);
    
    if (e.Args.Length >= 2 && e.Args[0] == "--send")
    {
        var filePath = e.Args[1];
        ShowSendDialog(filePath);
    }
}

private void ShowSendDialog(string filePath)
{
    // 채팅방 선택 화면 표시
    // 사용자가 채팅방 선택 → ChatService.SendFile(filePath)
}
```

#### 방법 D: 명령줄
```bash
# CMD/PowerShell에서
Noah_BisonPC.exe --send "C:\path\to\file.pdf"

# 다른 앱에서 호출 가능
```

#### 방법 E: 파일 연결 (Open with...)
```
.noahmsg 같은 자체 형식 만들고 등록
또는 기존 형식과 연결 (.pdf 우클릭 → "다른 앱으로 열기 → Noah")
```

### 2.2 보내기 (Noah → 다른 앱)

#### 방법 A: 메시지 우클릭
```csharp
// 채팅방 메시지 우클릭 컨텍스트 메뉴
<ContextMenu>
    <MenuItem Header="복사" Command="{Binding CopyCommand}" />
    <MenuItem Header="답장" Command="{Binding ReplyCommand}" />
    <MenuItem Header="다른 앱으로 공유..." Command="{Binding ShareExternalCommand}" />
    <MenuItem Header="저장..." Command="{Binding SaveAsCommand}" />
</ContextMenu>
```

```csharp
[RelayCommand]
private void ShareExternal(Message msg)
{
    if (msg.HasAttachment)
    {
        // Windows 공유 기본 메뉴 (Win10+)
        var dataPackage = new DataPackage();
        dataPackage.SetText(msg.Text);
        dataPackage.SetStorageItems(new[] { 
            await StorageFile.GetFileFromPathAsync(msg.AttachmentPath) 
        });
        DataTransferManager.ShowShareUI();
    }
    else
    {
        Clipboard.SetText(msg.Text);
        // "복사됨" 토스트
    }
}
```

#### 방법 B: 첨부 파일 "다른 이름으로 저장"
```csharp
[RelayCommand]
private async Task SaveAttachmentAs(Attachment att)
{
    var dialog = new SaveFileDialog
    {
        FileName = att.Filename,
        Filter = $"{att.Mime}|*.*"
    };
    
    if (dialog.ShowDialog() == true)
    {
        File.Copy(att.LocalPath, dialog.FileName, overwrite: true);
    }
}
```

#### 방법 C: 메시지 → 카카오톡 직접 전송
Windows 자체 공유 API 사용 (Win10+):

```csharp
DataTransferManager.GetForCurrentView().DataRequested += (sender, args) =>
{
    args.Request.Data.SetText(msg.Text);
    args.Request.Data.Properties.Title = "Noah 메시지";
};
DataTransferManager.ShowShareUI();
```

→ Windows가 자동으로 "공유" 다이얼로그 표시 → 카카오톡, 메일, 메모 등 선택.

---

## 3. 모바일 (Android)

### 3.1 받기 (다른 앱 → Noah)

#### Share Intent 등록 (핵심)

```xml
<!-- AndroidManifest.xml -->
<application>
    <activity android:name=".ShareReceiverActivity"
              android:exported="true">
        
        <!-- 텍스트 받기 -->
        <intent-filter>
            <action android:name="android.intent.action.SEND" />
            <category android:name="android.intent.category.DEFAULT" />
            <data android:mimeType="text/plain" />
        </intent-filter>
        
        <!-- 이미지 받기 -->
        <intent-filter>
            <action android:name="android.intent.action.SEND" />
            <category android:name="android.intent.category.DEFAULT" />
            <data android:mimeType="image/*" />
        </intent-filter>
        
        <!-- 비디오 받기 -->
        <intent-filter>
            <action android:name="android.intent.action.SEND" />
            <category android:name="android.intent.category.DEFAULT" />
            <data android:mimeType="video/*" />
        </intent-filter>
        
        <!-- 파일 받기 (PDF, 문서 등) -->
        <intent-filter>
            <action android:name="android.intent.action.SEND" />
            <category android:name="android.intent.category.DEFAULT" />
            <data android:mimeType="application/pdf" />
            <data android:mimeType="application/msword" />
            <data android:mimeType="application/vnd.openxmlformats-officedocument.*" />
            <data android:mimeType="application/zip" />
        </intent-filter>
        
        <!-- 어떤 파일이든 받기 -->
        <intent-filter>
            <action android:name="android.intent.action.SEND" />
            <category android:name="android.intent.category.DEFAULT" />
            <data android:mimeType="*/*" />
        </intent-filter>
        
        <!-- 여러 파일 받기 -->
        <intent-filter>
            <action android:name="android.intent.action.SEND_MULTIPLE" />
            <category android:name="android.intent.category.DEFAULT" />
            <data android:mimeType="*/*" />
        </intent-filter>
    </activity>
</application>
```

```csharp
// ShareReceiverActivity.cs
[Activity(Label = "Noah", NoHistory = true)]
[IntentFilter(new[] { Intent.ActionSend }, 
              Categories = new[] { Intent.CategoryDefault },
              DataMimeType = "*/*")]
public class ShareReceiverActivity : Activity
{
    protected override void OnCreate(Bundle savedInstanceState)
    {
        base.OnCreate(savedInstanceState);
        
        if (Intent?.Action == Intent.ActionSend)
        {
            HandleSingleShare(Intent);
        }
        else if (Intent?.Action == Intent.ActionSendMultiple)
        {
            HandleMultipleShare(Intent);
        }
        
        // 채팅방 선택 화면으로 이동
        StartActivity(typeof(MainActivity));
        Finish();
    }
    
    private void HandleSingleShare(Intent intent)
    {
        // 텍스트
        var text = intent.GetStringExtra(Intent.ExtraText);
        if (!string.IsNullOrEmpty(text))
        {
            ShareCache.PendingText = text;
        }
        
        // 파일/이미지
        var uri = intent.GetParcelableExtra(Intent.ExtraStream) as Android.Net.Uri;
        if (uri != null)
        {
            var localPath = CopyUriToLocal(uri);
            ShareCache.PendingFiles.Add(localPath);
        }
    }
    
    private string CopyUriToLocal(Android.Net.Uri uri)
    {
        var input = ContentResolver.OpenInputStream(uri);
        var fileName = GetFileName(uri);
        var localPath = Path.Combine(CacheDir.AbsolutePath, fileName);
        
        using var output = File.OpenWrite(localPath);
        input.CopyTo(output);
        
        return localPath;
    }
}
```

**사용**:
```
1. 갤러리에서 사진 선택
2. 공유 아이콘 탭
3. 공유 메뉴에 "Noah" 표시 (자동)
4. Noah 탭
5. Noah 채팅방 선택 화면
6. 채팅방 선택 → 자동 전송
```

### 3.2 보내기 (Noah → 다른 앱)

```csharp
// .NET MAUI Share API
[RelayCommand]
private async Task ShareMessage(Message msg)
{
    if (msg.HasAttachment)
    {
        await Share.RequestAsync(new ShareFileRequest
        {
            Title = msg.Text ?? "Noah 첨부",
            File = new ShareFile(msg.AttachmentPath, msg.AttachmentMime)
        });
    }
    else
    {
        await Share.RequestAsync(new ShareTextRequest
        {
            Title = "Noah 메시지",
            Text = msg.Text,
            Subject = "Noah에서 공유"
        });
    }
}
```

→ Android 시스템 공유 시트 자동 표시 → 카카오톡, 인스타, 메일 등 선택.

### 3.3 카메라 직접 촬영 → 전송

```csharp
[RelayCommand]
private async Task TakePhotoAndSend()
{
    var photo = await MediaPicker.CapturePhotoAsync();
    if (photo == null) return;
    
    var localPath = Path.Combine(FileSystem.CacheDirectory, photo.FileName);
    using var stream = await photo.OpenReadAsync();
    using var newStream = File.OpenWrite(localPath);
    await stream.CopyToAsync(newStream);
    
    await ChatService.SendFile(localPath);
}

[RelayCommand]
private async Task RecordVideoAndSend()
{
    var video = await MediaPicker.CaptureVideoAsync();
    if (video == null) return;
    
    var localPath = Path.Combine(FileSystem.CacheDirectory, video.FileName);
    using var stream = await video.OpenReadAsync();
    using var newStream = File.OpenWrite(localPath);
    await stream.CopyToAsync(newStream);
    
    await ChatService.SendFile(localPath);
}
```

### 3.4 갤러리에서 다중 선택

```csharp
[RelayCommand]
private async Task PickMultipleFromGallery()
{
    var files = await FilePicker.PickMultipleAsync(new PickOptions
    {
        FileTypes = FilePickerFileType.Images,
        PickerTitle = "사진 선택"
    });
    
    foreach (var file in files)
    {
        await ChatService.SendFile(file.FullPath);
    }
}
```

---

## 4. 통합 사용 시나리오

### 시나리오 1: 디버깅 도움 요청
```
1. 앱 크래시 발생 (다른 앱)
2. Android 시스템: 스크린샷 자동 캡처
3. 사용자: 스크린샷 → 공유 → Noah
4. Noah: "어느 채팅방?" 선택
5. 사용자: "크루"
6. Noah: 크루 채팅방에 스크린샷 + "이 에러 분석해줘"
7. 크루 (AI): 분석 후 답장
```

### 시나리오 2: 회사 자료 공유
```
1. 카카오톡으로 거래처가 PDF 보냄
2. 사용자: 카톡 PDF 미리보기 → 공유 → Noah
3. Noah: "방주" 채팅방 선택
4. AI들이 PDF 받음
5. 비손서버: "이 PDF 내용 요약해드릴게요"
6. 안목: "관련 자료 폴더에 저장했습니다"
```

### 시나리오 3: 협업 코딩
```
1. VSCode에서 코드 작성 중
2. 사용자: 코드 블록 복사 (Ctrl+C)
3. Noah PC 창에 붙여넣기 (Ctrl+V)
4. 자동으로 코드 스니펫으로 전송
5. 크루: "이 코드 리팩토링 제안드립니다 [수정 코드]"
6. 사용자: 크루 코드 복사 → VSCode에 붙여넣기
```

### 시나리오 4: 모니터링 알림
```
1. 비손서버: 주식 알림 자동 발송
2. Noah가 모바일 푸시 표시
3. 사용자: 알림 탭 → Noah → 차트 PDF 확인
4. 사용자: PDF → 공유 → 카카오톡 → 친구에게 보내기
```

---

## 5. 권한 처리

### Android
```xml
<!-- AndroidManifest.xml -->
<uses-permission android:name="android.permission.READ_EXTERNAL_STORAGE" />
<uses-permission android:name="android.permission.WRITE_EXTERNAL_STORAGE" />
<uses-permission android:name="android.permission.CAMERA" />
<uses-permission android:name="android.permission.RECORD_AUDIO" />
<uses-permission android:name="android.permission.READ_CONTACTS" />
<uses-permission android:name="android.permission.INTERNET" />
<uses-permission android:name="android.permission.POST_NOTIFICATIONS" />
```

### 런타임 권한 요청
```csharp
// 카메라 사용 전
var status = await Permissions.RequestAsync<Permissions.Camera>();
if (status != PermissionStatus.Granted)
{
    await DisplayAlert("권한 필요", "사진 촬영을 위해 카메라 권한이 필요합니다", "확인");
    return;
}

// 연락처 (선택)
var contactsStatus = await Permissions.RequestAsync<Permissions.ContactsRead>();
```

### Windows
WPF는 별도 권한 X. 그러나 일부 폴더 접근은 사용자 확인 필요:
- Documents, Downloads는 자유
- System32, Program Files는 관리자 권한 필요

---

## 6. 보안 고려사항

### 6.1 외부 입력 검증

```csharp
// 외부에서 받은 파일은 항상 검증
public async Task ReceiveExternalFile(string path)
{
    var info = new FileInfo(path);
    
    // 크기 제한
    if (info.Length > 100 * 1024 * 1024) // 100MB
    {
        throw new InvalidOperationException("파일이 너무 큽니다 (최대 100MB)");
    }
    
    // 확장자 화이트리스트 (또는 블랙리스트)
    var dangerous = new[] { ".exe", ".bat", ".cmd", ".scr", ".com", ".pif" };
    if (dangerous.Contains(info.Extension.ToLower()))
    {
        var confirm = await DisplayAlert(
            "위험한 파일", 
            "실행 가능한 파일입니다. 정말 전송하시겠습니까?", 
            "전송", "취소");
        if (!confirm) return;
    }
    
    // 파일명 sanitize
    var safeName = SanitizeFileName(info.Name);
    
    // 전송
    await ChatService.SendFile(path, displayName: safeName);
}
```

### 6.2 다른 앱이 보낸 데이터 신뢰

```csharp
// 외부 앱이 보낸 텍스트도 sanitize
var text = intent.GetStringExtra(Intent.ExtraText);
text = HtmlSanitizer.Sanitize(text); // HTML이 포함될 수 있음
```

### 6.3 임시 파일 정리

```csharp
// 외부 파일을 캐시에 복사한 경우, 전송 후 삭제
public async Task SendAndCleanup(string tempPath)
{
    try
    {
        await ChatService.SendFile(tempPath);
    }
    finally
    {
        if (tempPath.StartsWith(FileSystem.CacheDirectory))
        {
            File.Delete(tempPath);
        }
    }
}
```

---

## 7. 정리

### 가능한 공유 방법 매트릭스

| 방법 | Windows | Android |
|------|---------|---------|
| 드래그앤드롭 | ✅ | ❌ (모바일 X) |
| 클립보드 | ✅ | ✅ |
| 컨텍스트 메뉴 | ✅ (레지스트리) | ✅ (Share Intent) |
| 명령줄 인자 | ✅ | ❌ |
| 시스템 공유 시트 | ✅ (Win10+) | ✅ |
| 카메라 직접 | ⚠️ (USB 카메라) | ✅ |
| 다중 파일 | ✅ | ✅ |
| URL Scheme | ⚠️ | ✅ (`noah://`) |

### 사용자 가치

```
공유 기능이 있으면:
✅ 다른 앱과 자연스럽게 통합
✅ 카카오톡 같은 표준 UX
✅ 워크플로 끊김 없음
✅ Noah가 "메신저"가 아닌 "허브"가 됨

공유 기능이 없으면:
❌ Noah 안에서만 갇힘
❌ 사용자가 매번 파일 복사
❌ 다른 앱과 단절
```

---

*— agent_crew | 2026-04-13*
