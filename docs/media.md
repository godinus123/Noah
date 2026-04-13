# Noah — 미디어 처리 가이드

**작성자**: agent_crew
**문서 목적**: 이미지/비디오/썸네일 처리, 무게 분석, 그림판

---

## 1. 핵심 질문 답변

### Q: "썸네일 미리보기 무거워?"
**A: 가볍습니다.** 원본의 **0.01% ~ 1%** 크기.

### Q: "동영상 사진은 어떤가?"
**A: 사진은 가볍고, 비디오는 썸네일만 가벼움.** 비디오 재생은 OS에 위임.

### Q: "그림 메신저 창에서?"
**A: SkiaSharp로 가능, 추가 0 MB.**

---

## 2. 썸네일 무게 분석

### 2.1 사이즈 비교

| 종류 | 원본 | 썸네일 (200x200) | 압축률 |
|------|------|-----------------|--------|
| 사진 (4032x3024, 12MP) | 4 MB | 15 KB | **1/270** |
| 고화질 (8K) | 12 MB | 15 KB | **1/800** |
| 비디오 (1분, 1080p) | 100 MB | 첫 프레임 20 KB | **1/5,000** |
| 비디오 (10분, 4K) | 1 GB | 첫 프레임 20 KB | **1/50,000** |
| PDF (12페이지) | 2 MB | 첫 페이지 30 KB | **1/65** |
| DOCX (10페이지) | 500 KB | 텍스트 미리보기 1 KB | **1/500** |

### 2.2 채팅방 100개 메시지 시나리오

```
시나리오: 채팅방 진입 → 최근 100개 메시지 표시
구성:
  - 텍스트:    50개
  - 이미지:    30개
  - 비디오:    15개
  - PDF/문서:   5개

메모리 사용:
  텍스트:    50 × 2 KB    = 100 KB
  이미지썸네일: 30 × 15 KB  = 450 KB
  비디오썸네일: 15 × 20 KB  = 300 KB
  PDF썸네일:   5 × 30 KB  = 150 KB
  메타데이터:  100 × 1 KB  = 100 KB
  ─────────────────────────────
  총:                       ~1.1 MB

원본은 다운로드 안 함 → 클릭 시만
```

**부담 거의 없음**. 카카오톡 100개 메시지 = 약 30MB.

### 2.3 1000개 메시지로 확장

```
1000개 메시지 (이미지/비디오 비율 유지):
  ~11 MB

10,000개 (1년치):
  ~110 MB
```

10년치 메시지를 메모리에 다 캐시해도 1GB 정도. 매우 가벼움.

---

## 3. 이미지 처리

### 3.1 흐름

```
1. 사용자가 이미지 첨부 (카메라/갤러리/드래그)
   ↓
2. 클라이언트가 자동 처리:
   a. 원본 → 로컬 저장 (attachments/)
   b. 썸네일 200x200 생성
   c. 메타데이터 추출 (크기/EXIF)
   ↓
3. 메시지 전송:
   - 메타데이터 + 썸네일 (전체 ~30KB)
   - 원본은 첨부 ID로 참조
   ↓
4. 받는 쪽:
   a. 메시지 + 썸네일 즉시 표시 (인스턴트)
   b. 사용자가 클릭 → 원본 다운로드 (P2P 또는 서버)
```

### 3.2 썸네일 생성 코드

```csharp
// MAUI/WPF 공통 (SkiaSharp)
public static class ImageThumbnail
{
    public static byte[] Create(string sourcePath, int maxSize = 200)
    {
        using var input = File.OpenRead(sourcePath);
        using var original = SKBitmap.Decode(input);
        
        // 비율 유지 리사이즈
        var scale = Math.Min(
            (float)maxSize / original.Width,
            (float)maxSize / original.Height);
        var newWidth = (int)(original.Width * scale);
        var newHeight = (int)(original.Height * scale);
        
        using var resized = original.Resize(
            new SKImageInfo(newWidth, newHeight),
            SKFilterQuality.High);
        
        using var image = SKImage.FromBitmap(resized);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, 85);
        
        return data.ToArray();
    }
}
```

**SkiaSharp**: MAUI 기본 포함. WPF는 NuGet 추가 (1MB).

### 3.3 이미지 형식별 처리

| 형식 | 지원 | 비고 |
|------|------|------|
| JPEG | ✅ | 가장 일반적 |
| PNG | ✅ | 투명도 |
| WebP | ✅ | 차세대, 더 작음 |
| GIF (정적) | ✅ | 단일 프레임 |
| GIF (애니메이션) | ⚠️ | 첫 프레임만 썸네일 |
| HEIC (iPhone) | ✅ | iOS 사진 |
| BMP | ✅ | Windows |
| TIFF | ⚠️ | 큰 파일 (스캔용) |
| RAW (DSLR) | ⚠️ | 별도 라이브러리 |
| SVG | ⚠️ | 벡터, 별도 처리 |

### 3.4 EXIF 정보 처리

```csharp
public class ImageMetadata
{
    public int Width { get; set; }
    public int Height { get; set; }
    public DateTime? DateTaken { get; set; }
    public string CameraMake { get; set; }
    public string CameraModel { get; set; }
    public double? GpsLatitude { get; set; }
    public double? GpsLongitude { get; set; }
}

// 개인정보 보호: GPS는 기본 제거
public static byte[] StripGpsExif(byte[] imageBytes)
{
    // EXIF에서 GPS 태그 제거
    // 사용자 옵션으로 켤 수 있음
}
```

**기본 동작**: GPS 자동 제거 (개인정보 보호). 사용자가 켤 수 있음.

### 3.5 이미지 표시 UI

```
┌─────────────────────────────┐
│  ┌──┐                       │
│  │비│ 비손피씨             │
│  └──┘ ┌─────────────────┐ │
│       │                   │ │
│       │   [썸네일 200x200]│ │  ← 클릭 가능
│       │                   │ │
│       └─────────────────┘ │
│       photo.jpg · 4.2 MB  │
│       오후 3:42           │
└─────────────────────────────┘

클릭 시:
  → 풀스크린 이미지 뷰어
  → 핀치 줌
  → 좌우 스와이프 (이전/다음 이미지)
  → 저장 버튼
  → 공유 버튼
```

---

## 4. 비디오 처리

### 4.1 함정

비디오는 **썸네일은 가볍지만 재생/녹화가 무겁습니다**.

| 작업 | 부담 | 라이브러리 크기 |
|------|------|---------------|
| 첫 프레임 추출 (썸네일) | 가벼움 (50ms) | SkiaSharp + FFmpeg.Wrap |
| **인라인 재생** | 중간 (디코더) | **+20 MB (LibVLCSharp)** |
| **카메라 녹화** | 무거움 (인코더) | OS API (0 MB) |
| 트랜스코딩 (포맷 변환) | 매우 무거움 | +30 MB |
| 메모리 (재생 중) | 100~300 MB | - |

### 4.2 Noah의 비디오 전략

```
Phase 1B (가벼움 우선):
  ✅ 비디오 첨부 받기
  ✅ 첫 프레임 썸네일 표시
  ✅ "재생" 버튼 → OS 기본 비디오 플레이어로 위임
  ❌ 자체 인라인 재생 X
  ❌ 카메라 녹화 X (Phase 2)

Phase 2 (필요 시):
  + 자체 인라인 재생 (LibVLCSharp, +20MB)
  + 카메라로 녹화 (Plugin.Maui.MediaElement)
  + 짧은 비디오 자동 재생 (1분 미만)

이유: LibVLCSharp 추가는 +20 MB로 부담 큼.
     Phase 1B에는 OS 위임으로 충분.
```

### 4.3 비디오 라이브러리 비교

| 라이브러리 | 크기 | 라이선스 | 기능 |
|----------|------|---------|------|
| **OS 기본 (Intent.ACTION_VIEW)** | 0 MB | - | 외부 앱 위임 ✅ |
| ExoPlayer (Android) | +5 MB | Apache 2 | 한정적 |
| LibVLCSharp | +20 MB | LGPL | 모든 코덱 |
| FFmpeg.AutoGen | +30 MB | LGPL | 트랜스코딩 |
| Plugin.Maui.MediaElement | +3 MB | MIT | 기본 재생 |

**Phase 1B 선택**: OS 기본 (0 MB)
**Phase 2 후보**: Plugin.Maui.MediaElement (+3 MB, 가벼움)

### 4.4 첫 프레임 썸네일 추출

#### Android
```csharp
// Android.Media.MediaMetadataRetriever
public byte[] ExtractVideoThumbnail(string videoPath)
{
    var retriever = new MediaMetadataRetriever();
    retriever.SetDataSource(videoPath);
    
    // 첫 프레임 (1초 시점)
    var bitmap = retriever.GetFrameAtTime(1_000_000); // microseconds
    
    using var stream = new MemoryStream();
    bitmap.Compress(Bitmap.CompressFormat.Jpeg, 85, stream);
    
    retriever.Release();
    return stream.ToArray();
}
```

#### Windows
```csharp
// Windows.Media.MediaProperties + ThumbnailTool
public async Task<byte[]> ExtractVideoThumbnail(string videoPath)
{
    var file = await StorageFile.GetFileFromPathAsync(videoPath);
    var thumbnail = await file.GetThumbnailAsync(ThumbnailMode.VideosView, 200);
    
    using var stream = thumbnail.AsStreamForRead();
    using var ms = new MemoryStream();
    await stream.CopyToAsync(ms);
    return ms.ToArray();
}
```

### 4.5 비디오 표시 UI

```
┌─────────────────────────────┐
│  ┌──┐                       │
│  │비│ 비손피씨             │
│  └──┘ ┌─────────────────┐ │
│       │                   │ │
│       │   [썸네일 320x180]│ │
│       │      ▶           │ │  ← 재생 아이콘 오버레이
│       │                   │ │
│       └─────────────────┘ │
│       video.mp4 · 25 MB  │
│       1:23 길이           │
│       오후 3:42           │
└─────────────────────────────┘

▶ 클릭 시:
  Phase 1B: OS 기본 비디오 플레이어 호출
  Phase 2: 인라인 재생 또는 풀스크린 모달
```

### 4.6 짧은 GIF/Lottie 애니메이션

```csharp
// Lottie (애니메이션, 가벼움)
- 라이브러리: LottieXamarin / SkiaSharp.Extended.Lottie
- 크기: +2 MB
- 사용: 이모티콘, 짧은 액션
- 가능: Phase 1B 추가 가능
```

---

## 5. 그림판 (Drawing)

### 5.1 SkiaSharp으로 구현

```csharp
// MAUI: SkiaSharp.Views.Maui (이미 포함)
<skia:SKCanvasView x:Name="canvas"
                   PaintSurface="OnPaintSurface"
                   EnableTouchEvents="True"
                   Touch="OnTouch" />
```

```csharp
private SKPath _currentPath;
private List<(SKPath Path, SKColor Color, float StrokeWidth)> _strokes = new();

private void OnTouch(object sender, SKTouchEventArgs e)
{
    switch (e.ActionType)
    {
        case SKTouchAction.Pressed:
            _currentPath = new SKPath();
            _currentPath.MoveTo(e.Location);
            break;
        
        case SKTouchAction.Moved:
            _currentPath?.LineTo(e.Location);
            ((SKCanvasView)sender).InvalidateSurface();
            break;
        
        case SKTouchAction.Released:
            if (_currentPath != null)
            {
                _strokes.Add((_currentPath, _selectedColor, _selectedWidth));
                _currentPath = null;
            }
            break;
    }
    e.Handled = true;
}

private void OnPaintSurface(object sender, SKPaintSurfaceEventArgs e)
{
    var canvas = e.Surface.Canvas;
    canvas.Clear(SKColors.White);
    
    foreach (var stroke in _strokes)
    {
        using var paint = new SKPaint
        {
            Color = stroke.Color,
            StrokeWidth = stroke.StrokeWidth,
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            IsAntialias = true
        };
        canvas.DrawPath(stroke.Path, paint);
    }
    
    if (_currentPath != null)
    {
        using var paint = new SKPaint
        {
            Color = _selectedColor,
            StrokeWidth = _selectedWidth,
            Style = SKPaintStyle.Stroke,
            StrokeCap = SKStrokeCap.Round,
            IsAntialias = true
        };
        canvas.DrawPath(_currentPath, paint);
    }
}

// 저장 → 메시지로 전송
[RelayCommand]
private async Task SaveAndSend()
{
    using var bitmap = new SKBitmap((int)canvas.CanvasSize.Width, 
                                    (int)canvas.CanvasSize.Height);
    using var skCanvas = new SKCanvas(bitmap);
    skCanvas.Clear(SKColors.White);
    
    foreach (var stroke in _strokes)
    {
        using var paint = new SKPaint { /* ... */ };
        skCanvas.DrawPath(stroke.Path, paint);
    }
    
    using var image = SKImage.FromBitmap(bitmap);
    using var data = image.Encode(SKEncodedImageFormat.Png, 100);
    
    var path = Path.Combine(FileSystem.CacheDirectory, $"draw_{Guid.NewGuid()}.png");
    using (var stream = File.OpenWrite(path))
    {
        data.SaveTo(stream);
    }
    
    await ChatService.SendFile(path);
}
```

### 5.2 그림판 기능

```
✅ 자유 그리기 (펜)
✅ 색상 선택 (8가지)
✅ 굵기 조절 (3가지)
✅ 지우개
✅ 실행 취소 (Undo)
✅ 다시 실행 (Redo)
✅ 전체 지우기
✅ 배경 색
⚠️ 도형 (사각형/원/직선) - Phase 2
⚠️ 텍스트 추가 - Phase 2
❌ 레이어 (포토샵 수준) - X
```

### 5.3 사용 사례

```
1. 빠른 스케치 → 디자인 아이디어 공유
2. 손글씨 → "여기 봐" 화살표
3. 화면 캡처 위에 표시 → 디버깅
4. 어린이 그림 → 가족 채팅
```

### 5.4 무게

```
SkiaSharp: 이미 MAUI에 포함 (모바일 +0 MB)
WPF: NuGet (+1 MB)

작업 시간: 5h
```

---

## 6. 이미지 캡션 (메시지 본문)

이미지/비디오에 텍스트 추가:

```
┌─────────────────────────────┐
│       [썸네일 200x200]    │
│  ┌─────────────────────┐ │
│  │ 이번 주 차트 분석   │ │  ← 캡션
│  │ 결과입니다.          │ │
│  └─────────────────────┘ │
│  chart.png · 230 KB      │
└─────────────────────────────┘
```

데이터 모델:
```sql
CREATE TABLE messages (
    msg_id TEXT PRIMARY KEY,
    type TEXT,            -- 'image_with_caption'
    text TEXT,            -- 캡션 (없으면 NULL)
    attachment_id TEXT,   -- 이미지
    ...
);
```

---

## 7. 메모리 관리 (LRU 캐시)

### 7.1 무한 캐시 방지

```csharp
public class ThumbnailCache
{
    private readonly LRUCache<string, byte[]> _cache;
    private const int MAX_CACHE_SIZE_MB = 50;
    
    public ThumbnailCache()
    {
        _cache = new LRUCache<string, byte[]>(
            maxSize: MAX_CACHE_SIZE_MB * 1024 * 1024,
            sizeFunc: bytes => bytes.Length);
    }
    
    public byte[] GetOrLoad(string thumbnailId)
    {
        if (_cache.TryGet(thumbnailId, out var cached))
            return cached;
        
        var bytes = LoadFromDisk(thumbnailId);
        _cache.Add(thumbnailId, bytes);
        return bytes;
    }
}
```

### 7.2 원본 이미지는 캐시 X

```
원본 이미지: 디스크에만 저장
화면 표시: 사용자가 클릭한 것만 임시 메모리
화면 닫으면 즉시 GC
```

### 7.3 메모리 모니터

```csharp
// 모바일은 메모리 부족 시 OS가 경고
protected override void OnLowMemory()
{
    base.OnLowMemory();
    
    // 캐시 50% 비우기
    ThumbnailCache.Instance.Trim(0.5);
    
    // GC 강제
    GC.Collect();
    GC.WaitForPendingFinalizers();
}
```

---

## 8. 디스크 관리

### 8.1 자동 정리

```
규칙:
- 채팅방 내 미표시 메시지의 첨부: 30일 후 자동 삭제
- 캐시 폴더: 7일 후 자동 삭제
- 전체 첨부 폴더 크기 > 5GB 시: 가장 오래된 것부터 삭제
```

```csharp
public class DiskCleanupService
{
    public async Task RunWeeklyAsync()
    {
        var attachmentsDir = Path.Combine(AppInfo.DataPath, "attachments");
        var totalSize = GetDirectorySize(attachmentsDir);
        
        if (totalSize > 5L * 1024 * 1024 * 1024) // 5GB
        {
            await CleanupOldestUntil(targetSize: 3L * 1024 * 1024 * 1024); // 3GB까지
        }
        
        // 캐시 7일 이상 삭제
        var cacheDir = Path.Combine(AppInfo.DataPath, "cache");
        DeleteOlderThan(cacheDir, TimeSpan.FromDays(7));
    }
}
```

### 8.2 사용자 수동 관리

```
설정 → 저장공간:
  - 첨부 파일 사용량: 2.4 GB
  - 캐시: 150 MB
  - [캐시 비우기]
  - [오래된 첨부 정리]
  - [전체 데이터 삭제]
```

---

## 9. 성능 최적화

### 9.1 이미지 lazy load

```csharp
// 채팅방 진입 시: 텍스트 + 썸네일만
// 사용자가 스크롤하면서 화면에 보이는 것만 로드
<CollectionView ItemsSource="{Binding Messages}"
                RemainingItemsThreshold="5"
                RemainingItemsThresholdReached="LoadMoreMessages">
    <CollectionView.ItemTemplate>
        <DataTemplate>
            <Image Source="{Binding ThumbnailSource}"
                   IsLoaded="OnImageLoaded" />
        </DataTemplate>
    </CollectionView.ItemTemplate>
</CollectionView>
```

### 9.2 이미지 압축 (전송 전)

```csharp
public byte[] CompressForSending(string originalPath, long maxSizeBytes = 1_000_000)
{
    var bytes = File.ReadAllBytes(originalPath);
    if (bytes.Length <= maxSizeBytes) return bytes;
    
    // 1MB 초과면 압축
    using var input = SKBitmap.Decode(bytes);
    
    int quality = 85;
    while (true)
    {
        using var image = SKImage.FromBitmap(input);
        using var data = image.Encode(SKEncodedImageFormat.Jpeg, quality);
        var compressed = data.ToArray();
        
        if (compressed.Length <= maxSizeBytes || quality < 30)
            return compressed;
        
        quality -= 10;
    }
}
```

### 9.3 점진적 다운로드 (Progressive JPEG)

```
큰 이미지 다운로드 시:
  1. 흐릿한 저화질 먼저 표시 (10 KB)
  2. 점차 선명해짐 (50 KB)
  3. 완전한 해상도 (500 KB)

이러면 사용자가 기다리는 느낌 적음.
```

---

## 10. 정리 — 무게 결론

### Phase 1B 추가 시 무게

```
+ 이미지 처리 (썸네일):     0 MB (SkiaSharp 기본)
+ 비디오 썸네일:           0 MB (OS API)
+ 비디오 재생:             0 MB (OS 위임)
+ 그림판:                  0 MB (SkiaSharp 기본)
+ 캡션:                   0 MB
─────────────────────────────────
모바일 추가:              0 MB
PC 추가:                  +1 MB (SkiaSharp WPF)

메모리 (실행 중):
+ 썸네일 캐시 (100개):    1 MB
+ 그림판 작업:            5 MB
+ 비디오 썸네일 캐시:     1 MB
─────────────────────────────────
+ 7 MB
```

**완전히 가벼움**. 이미지/비디오/그림판 전부 추가해도 부담 X.

### Phase 2 (자체 비디오 재생) 추가 시

```
+ Plugin.Maui.MediaElement: +3 MB (모바일/PC)
+ 메모리 (재생 중):        +100 MB (잠시)

수용 가능.
```

### Phase 2 (트랜스코딩) 추가 시

```
+ FFmpeg.AutoGen:         +30 MB
+ 메모리:                 +500 MB

부담 큼. 안 함.
```

---

## 11. 결정 사항

### Phase 1B 추가 권장

```
✅ 이미지 썸네일 미리보기  (0 MB, 4h)
✅ 이미지 풀스크린 뷰어    (0 MB, 3h)
✅ 비디오 썸네일           (0 MB, 2h)
✅ 비디오 OS 위임 재생     (0 MB, 1h)
✅ 그림판 메시지            (0 MB, 5h)
✅ 이미지 캡션             (0 MB, 1h)
─────────────────────────────────
+ 16h, 무게 0
```

### Phase 2 추후 검토

```
⚠️ 자체 비디오 재생       (+3 MB, 8h)
⚠️ 카메라 녹화            (0 MB, 6h, OS API)
❌ 비디오 트랜스코딩      (+30 MB) — 안 함
❌ RAW 이미지            (+5 MB) — 우선순위 낮음
```

---

## 12. 사용자 가치

```
가벼운 추가 기능 (16h):

📸 사진 보내기:
   카카오톡과 동일한 UX
   썸네일 → 클릭 → 풀스크린

🎬 비디오 보내기:
   썸네일 + 재생 버튼
   재생은 OS 기본 플레이어
   
🎨 그림 그리기:
   빠른 스케치
   손글씨
   화면 위 표시
   
이게 다 0 MB 추가로 가능.
```

---

*— agent_crew | 2026-04-13*
