# 🕊️ Noah

> **방주에서 보낸 메시지** — 비둘기처럼 빠른 P2P 메신저

[![Status](https://img.shields.io/badge/status-design--complete-blue)](.)
[![Phase](https://img.shields.io/badge/phase-1A%20pending-yellow)](.)
[![License](https://img.shields.io/badge/license-Private-red)](.)

---

## 📖 이름의 의미

> "비둘기를 자기에게서 내놓아 ... 저녁때에 비둘기가 그에게로 돌아왔는데  
> 그 입에 감람나무 새 잎사귀가 있는지라  
> 이에 노아가 땅에 물이 줄어든 줄을 알았으며"  
> — **창세기 8:11**

**Noah**는 노아의 방주에서 비둘기가 메시지를 전달한 것에서 영감을 받은 메신저입니다.  
디지털 시대의 전서구가 되겠습니다.

---

## ✨ 핵심 가치

- **🕊️ 빠른 통신** — WebRTC P2P로 80ms 응답
- **🔒 개인정보 보호** — 서버는 메시지를 영구 저장하지 않음 (배달 후 삭제)
- **🤖 AI 통합** — 사람과 AI 에이전트가 같은 채팅방에서 협업
- **📈 확장성** — 단일 서버로 10만 사용자 처리

---

## 🏗️ 아키텍처

```
┌───────────────┐       ┌──────────────────┐       ┌──────────────┐
│  Mobile MAUI  │       │  Linux Server    │       │  PC WPF      │
│  (Android)    │◄─────►│  (Node.js + ws)  │◄─────►│  (Windows)   │
│               │       │                  │       │              │
│  - 안목       │       │  - 시그널링      │       │  - 비손피씨  │
└───────┬───────┘       │  - 메시지 큐      │       └──────┬───────┘
        │               │  - FCM 푸시      │              │
        │               └────────┬─────────┘              │
        │                        │                        │
        └─── WebRTC P2P ─────────┼──── (DataChannel) ─────┘
                                 │
                          ┌──────▼───────┐
                          │  FCM (Push)  │
                          └──────────────┘
```

---

## 🛶 방주 [The Ark]

AI 에이전트들이 모이는 메인 채팅방:

```
🛶 방주 [The Ark]
├── 🕊️ 크루 (지휘)         — Web (claude.ai)
├── 🐦 안목 (모바일)         — Android Studio
├── 🦅 비손피씨 (Windows)   — Visual Studio
├── 🦉 비손서버 (Linux)     — VPS Server
└── 👤 이효승 (사용자)
```

각 AI는 새의 종류로 비유. 노아의 방주에 모인 새들 = AI 협업 채팅방.

---

## 📊 핵심 사양

| 항목 | 결정 |
|------|------|
| **통신** | WebRTC P2P + WebSocket fallback |
| **서버 데이터** | 큐만 (배달 후 삭제, 7일 만료) |
| **클라이언트 DB** | `messages.db` + `attachments.db` 분리 |
| **시간 정렬** | `server_seq` (전역 순번) |
| **사용자** | Phase 1 단순화 (1명) |
| **디바이스** | 빌드 시 `AssemblyName`으로 EXE 분리 |
| **푸시** | FCM (Android) + Toast (Windows) |
| **UI** | Syncfusion SfChat (모바일) + HandyControl (PC) |
| **테마** | 카카오톡 스타일 (한국 사용자 친숙) |

---

## 🗓️ Phase 분할

### Phase 1A — 사람 간 메신저 (1주, ~28h)
- 1:1 채팅
- 텍스트 + 첨부 파일
- 멀티 디바이스
- WebRTC P2P + 서버 폴백
- FCM 푸시

### Phase 1B — AI 에이전트 통합 (3일, ~14h)
- AI 에이전트 4명 (크루/안목/비손피씨/비손서버)
- 그룹 채팅 (사람 + AI)
- AI SDK (Python, Node.js)

### Phase 2 — P2P DB Sync (1주, ~12h)
- 새 디바이스가 기존 디바이스에서 히스토리 가져오기
- 증분 sync (last_seq 기반)

### Phase 3 — 보안 강화 (선택)
- E2E 암호화
- 디바이스별 키쌍

---

## 📁 프로젝트 구조

```
Noah/
├── README.md                    ← 이 파일
├── docs/
│   ├── Noah_design.md           ← 종합 설계 문서 (1300+ 줄)
│   ├── api_spec.md              ← API 명세
│   └── ui_design.md             ← UI 가이드
├── server/                      ← Linux Node.js 서버 (비손서버)
│   ├── package.json
│   ├── server.js
│   └── ...
├── mobile/                      ← MAUI Android (안목)
│   ├── Noah.csproj
│   └── ...
├── pc/                          ← WPF Windows (비손피씨)
│   ├── Noah.csproj
│   └── ...
├── ai_sdk/                      ← AI 에이전트 SDK
│   ├── python/
│   └── nodejs/
├── icons/                       ← 앱 아이콘 모음
│   ├── Noah_512.png
│   ├── Noah.ico
│   └── ...
└── channel/                     ← 에이전트 통신 메시지
    └── ...
```

---

## 🚀 시작 (개발자용)

### 사전 요구사항

- **서버**: Node.js 20+, SQLite
- **모바일**: .NET 9 SDK + MAUI 워크로드 + Android SDK
- **PC**: .NET 8 SDK + Visual Studio 2022 (또는 Rider)

### 빌드

각 컴포넌트의 README를 참조하세요:
- [`server/README.md`](server/README.md)
- [`mobile/README.md`](mobile/README.md)
- [`pc/README.md`](pc/README.md)

---

## 👥 팀

| 역할 | 담당 | 작업 |
|------|------|------|
| 🕊️ **크루** | agent_crew (Web) | 설계, 코드 작성, 조율 |
| 🐦 **안목** | agent_anmok (Android Studio) | 모바일 빌드, 실기기 테스트 |
| 🦅 **비손피씨** | agent_bison_pc (Visual Studio) | PC 빌드, Windows 통합 |
| 🦉 **비손서버** | agent_bison_server (Linux) | 서버 운영, 인프라 |
| 👤 **이효승** | (사용자) | 기획, 의사결정, 테스트 |

---

## 📜 라이선스

Private — 무단 복제 금지

---

## 🙏 영감

- **창세기 8장** — 노아의 방주와 비둘기
- **카카오톡** — UI/UX 표준
- **Signal** — 개인정보 보호 철학
- **Discord** — AI 통합 가능성
- **WhatsApp** — 전화번호 기반 가입

---

*"비둘기처럼 빠르게, 방주처럼 안전하게."*  
— Noah Team
