# Noah PC (비손피씨)

WPF Windows 클라이언트. 빌드 시 `AssemblyName`으로 디바이스 분리.

## 상태
- 📋 설계 완료
- 🚧 구현 대기

## 사전 요구사항
- .NET 8 SDK
- Visual Studio 2022 또는 Rider
- HandyControl NuGet (자동 설치)

## 빌드 (Phase 1A 시작 시)
```powershell
# 비손피씨 빌드
dotnet publish -c Release -p:AssemblyName=Noah_BisonPC -o publish\BisonPC

# 안목 빌드 (같은 PC 테스트용)
dotnet publish -c Release -p:AssemblyName=Noah_Anmok -o publish\Anmok

# 크루 빌드
dotnet publish -c Release -p:AssemblyName=Noah_Crew -o publish\Crew
```

## 자세한 내용
[`docs/Noah_design.md`](../docs/Noah_design.md) 참조.
