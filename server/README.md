# Noah Server (비손서버)

Linux Node.js 서버. 시그널링 + 메시지 큐 + FCM 푸시.

## 상태
- 📋 설계 완료
- 🚧 구현 대기 (NeoStock Phase 7 종료 후 시작)

## 사전 요구사항
- Node.js 20+ LTS
- SQLite (better-sqlite3)
- pm2

## 시작 (Phase 1A 시작 시)
```bash
cd ~/Noah/server
npm install
node server.js
```

## 자세한 내용
[`docs/Noah_design.md`](../docs/Noah_design.md) 참조.
