# Contributing to NOAH

> **NOAH = Networked Operations Agent Hub**  
> A real-time messaging platform for human + AI multi-agent collaboration.

NOAH는 공동 프로젝트입니다. 사람 개발자와 AI 에이전트 모두 기여할 수 있습니다.

---

## 🎯 프로젝트 구조

```
NOAH/
├── server/      ← Linux Node.js 서버 (담당: 비손서버)
├── mobile/      ← MAUI Android 앱 (담당: 안목)
├── pc/          ← WPF Windows 앱 (담당: 비손피씨)
├── ai_sdk/      ← AI 에이전트 SDK (담당: 크루)
├── docs/        ← 설계 문서 (담당: 크루)
└── tests/       ← 통합 테스트 (모두)
```

---

## 👥 팀 (4 에이전트 + 1 사람)

| 역할 | 환경 | 주 책임 |
|------|------|---------|
| 🕊️ **크루** | claude.ai (Web) | 설계, 조율, 코드 리뷰, 문서 |
| 🐦 **안목** | Android Studio | 모바일 빌드, MAUI 클라이언트 |
| 🦅 **비손피씨** | Visual Studio | WPF 클라이언트, Windows 통합 |
| 🦉 **비손서버** | Linux SSH | Node.js 서버, 인프라 |
| 👤 **이효승** | (사람) | 비전, 의사결정, 테스트 |

각 에이전트는 자기 전문 영역을 맡고, 크루가 전체 조율.

---

## 🌳 브랜치 전략

```
main          ← 안정 (배포 가능)
└── dev       ← 통합 (모든 PR 여기로)
    ├── feature/server-init       (비손서버)
    ├── feature/mobile-init       (안목)
    ├── feature/pc-init           (비손피씨)
    ├── feature/server-websocket  (비손서버 - 작업 단위)
    ├── feature/mobile-chat-ui    (안목)
    └── docs/contributing         (크루)
```

### 브랜치 명명
```
feature/{component}-{topic}    ← 새 기능
fix/{component}-{bug}          ← 버그 수정
docs/{topic}                   ← 문서
refactor/{component}-{topic}   ← 리팩토링
test/{component}-{topic}       ← 테스트
```

예: `feature/server-websocket`, `fix/mobile-crash-on-startup`

---

## 📝 작업 흐름

### 1. Issue 확인
```
1. https://github.com/godinus123/Noah/issues 방문
2. 'help wanted' 또는 자기 담당 라벨로 필터
3. 시작 가능한 issue 선택
4. Issue에 자기 담당으로 댓글
```

### 2. 브랜치 생성
```bash
git checkout dev
git pull origin dev
git checkout -b feature/server-websocket
```

### 3. 작업 + 커밋
```bash
# 작업 후
git add -A
git commit -m "feat(server): add WebSocket echo endpoint

- ws server on port 3001
- echo back received text
- basic connection handling

closes #5"
```

### 4. Push + PR
```bash
git push origin feature/server-websocket
```

GitHub에서 PR 생성:
- Base: `dev`
- Compare: `feature/server-websocket`
- 제목: `[server] Add WebSocket echo endpoint`
- 본문: 변경 사항, 테스트 결과, 관련 issue
- Reviewer: agent_crew (또는 다른 에이전트)

### 5. 리뷰 + 머지
```
크루 리뷰 → 승인 → dev에 머지
주기적으로 dev → main 머지 (안정 시점)
```

---

## ✏️ 커밋 메시지 컨벤션

[Conventional Commits](https://www.conventionalcommits.org/) 따름:

```
<type>(<scope>): <subject>

<body>

<footer>
```

### Type
- `feat`: 새 기능
- `fix`: 버그 수정
- `docs`: 문서 변경
- `style`: 코드 포맷팅 (기능 변경 없음)
- `refactor`: 리팩토링
- `test`: 테스트 추가/수정
- `chore`: 빌드/설정 변경
- `perf`: 성능 개선

### Scope
- `server`, `mobile`, `pc`, `ai_sdk`, `docs`, `ci`

### 예시
```
feat(server): add device registration endpoint

POST /api/devices/register accepts device_name and device_type,
returns device_id and auth token.

closes #3
```

```
fix(mobile): resolve crash when receiving large attachment

The MemoryStream was not being disposed, causing OOM on files
larger than 50MB. Wrapped in using statement.

fixes #42
```

```
docs(contributing): add commit message convention
```

---

## 🧪 테스트

### 작업 전
```bash
# 모든 테스트 통과 확인
cd server && npm test
cd pc && dotnet test
cd mobile && dotnet test
```

### 작업 후
```bash
# 새 테스트 추가
- 단위 테스트 (xUnit, Jest)
- 통합 테스트 (TestDevice 사용)
- 수동 검증 결과를 PR 본문에 명시
```

자세한 테스트 가이드: [`docs/testing.md`](docs/testing.md)

---

## 🎨 코딩 스타일

### C# (.NET MAUI/WPF)
- 표준 Microsoft 스타일
- `var` 사용 권장
- async/await (sync API 지양)
- nullable reference types 활성화
- StyleCop 또는 .editorconfig 따름

### JavaScript/TypeScript (서버)
- ES6+ 문법
- async/await (콜백 지양)
- Prettier 자동 포맷
- ESLint 통과

### Python (AI SDK)
- PEP 8
- type hints
- black 자동 포맷

---

## 📦 PR 체크리스트

PR 생성 전 확인:

- [ ] 자기 브랜치는 dev에서 분기
- [ ] 커밋 메시지가 컨벤션 따름
- [ ] 새 기능에 테스트 추가
- [ ] 모든 테스트 통과
- [ ] 문서 업데이트 (필요 시)
- [ ] 빌드 경고 0개
- [ ] 자기 코드 self-review 완료

---

## 🎯 우선순위 라벨

- 🔥 **P0 (Critical)**: 즉시 처리
- 🟠 **P1 (High)**: 이번 Phase 안에
- 🟡 **P2 (Medium)**: 다음 Phase
- 🟢 **P3 (Low)**: 시간 날 때

---

## 🚦 Phase별 작업

### Phase 0 — 미니멀 (3일)
**목표**: "Hello"가 두 디바이스 사이를 오가는 것

핵심 작업:
- [ ] #1 [server] WebSocket echo server (Node.js)
- [ ] #2 [pc] WPF 미니멀 클라이언트 (입력+표시)
- [ ] #3 [mobile] MAUI 미니멀 클라이언트
- [ ] #4 [docs] 빌드 안내

### Phase 1A — 기본 메신저 (1주)
- [ ] #5 [server] SQLite + 디바이스 등록
- [ ] #6 [server] 메시지 큐 (오프라인)
- [ ] #7 [pc] 채팅방 UI
- [ ] #8 [pc] SQLite 로컬 저장
- [ ] #9 [mobile] 채팅방 UI
- [ ] #10 [mobile] FCM 푸시
- [ ] #11 [all] 첨부 파일 업로드/다운로드
- [ ] #12 [server] WebRTC 시그널링

### Phase 1B — 고급 기능 (1주)
- [ ] #13 [all] 그룹 채팅
- [ ] #14 [all] 답장 인용
- [ ] #15 [all] 이모지 반응
- [ ] #16 [all] HTML 메시지 (sanitize)
- [ ] #17 [all] PDF 뷰어 (PDFium)
- [ ] #18 [all] AI 에이전트 통합
- [ ] #19 [ai_sdk] Python SDK
- [ ] #20 [ai_sdk] Node.js SDK

### Phase 2 — 풀 기능 (1주)
- [ ] #21 P2P DB sync
- [ ] #22 DOCX/XLSX/PPTX 뷰어
- [ ] #23 음성 메시지
- [ ] #24 다크 모드
- [ ] #25 그림판
- [ ] #26 자동화 테스트
- [ ] #27 부하 테스트

---

## 🤖 AI 에이전트 기여 가이드

크루/안목/비손피씨/비손서버는 사람 개발자와 똑같이 기여 가능.

### 다른 점
- AI는 commit을 직접 push 가능 (사람의 PR 리뷰 후)
- AI 작성 코드는 commit 메시지에 `co-authored-by: agent_xxx`
- 사람 코드와 동일한 품질 기준
- AI 간 대화는 commit 메시지에 요약

### AI Co-author 예시
```
feat(server): add seq_counter

Implements monotonic sequence number generation for messages,
ensuring consistent ordering across all devices.

closes #7
co-authored-by: agent_crew <crew@noah.ai>
co-authored-by: agent_bison_server <bison@noah.ai>
```

---

## 🆘 도움 요청

막힐 때:
1. Issue에 댓글로 질문
2. 크루에게 멘션 (`@agent_crew`)
3. Discord/Slack (TBD)

---

## 📄 라이선스

기여하시면 [MIT License](LICENSE)에 동의하는 것으로 간주됩니다.

```
Copyright (c) 2026 John H. Lee (이효승)
```

---

## 🙏 감사합니다

NOAH는 사람과 AI가 함께 만드는 프로젝트입니다.  
당신의 기여가 더 나은 협업 도구를 만듭니다.

*— NOAH Team*
