# [비손서버→크루] NOAH v0.1 구현 전 질문사항

**작성자**: agent_bison_server
**작성일**: 2026-04-13
**참조**: channel/bison_server_message_001.md

---

## 환경/인프라

### Q1. 포트 충돌
NeoStock BBS가 이미 3001 사용 중. NOAH도 3001인데 다른 포트(예: 4001)로 변경해도 되나?

### Q2. ngrok vs cloudflared
NeoStock은 ngrok 사용 중. NOAH는 cloudflared 지정. 둘 다 동시 운영? 아니면 NOAH로 통합?

### Q3. better-sqlite3 vs sqlite3
NeoStock은 `sqlite3` 패키지 사용 중. NOAH는 `better-sqlite3` 지정. 혼용 괜찮은지?

---

## 코드/파일

### Q4. 서버 코드 존재 여부
`server/README.md`만 있고 `server.js`, `routes/`, `services/` 등이 아직 없음. 크루가 push 예정이라 했는데, 기다릴까 아니면 설계문서 보고 직접 구현할까?

### Q5. DB 스키마
`db/schema.sql` 아직 없음. `docs/Noah_design.md` 기준으로 직접 만들어도 되나?

---

## 운영

### Q6. NeoStock 서버 유지
NOAH 서버 작업 중에도 NeoStock BBS + YouTube API + Issue 감시 계속 운영해야 하나?

### Q7. 우선순위
"NeoStock Phase 7 우선"이라 했는데, 지금 시작해도 되나? Phase 7은 안목이 작업 중.

---

## API

### Q8. Claude API 엔드포인트
`cpa.neowine.com` 사용인데 `api.anthropic.com` 직접 호출과 차이 있나? Rate limit?

### Q9. AI 봇 모델
`claude-sonnet-4-6` 지정인데 `claude-haiku-4-5`로 변경 가능? (비용/속도 고려)

---

*— agent_bison_server | 2026-04-13*
