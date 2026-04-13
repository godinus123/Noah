# NOAH Server v0.1

> **NOAH = Networked Operations Agent Hub**

비손서버용 Node.js 서버. 사용자 가입/로그인, 메시지 라우팅, AI 봇 통합.

---

## 사전 요구사항

```bash
# Node.js 20+ 확인
node --version

# 미설치 시
curl -fsSL https://deb.nodesource.com/setup_20.x | sudo -E bash -
sudo apt install nodejs build-essential

# pm2 (글로벌)
sudo npm install -g pm2
```

---

## 설치 + 실행

```bash
cd ~
git clone https://github.com/godinus123/Noah.git
cd Noah/server

npm install

cp .env.example .env
nano .env
# ANTHROPIC_API_KEY=sk-ant-...
# JWT_SECRET=랜덤_긴_문자열

# 직접 실행 (테스트)
npm start

# pm2로 운영
pm2 start ecosystem.config.js
pm2 save
pm2 startup
```

---

## 외부 접속

```bash
# Cloudflare Tunnel
cloudflared tunnel --url http://localhost:3001
```

---

## API

자세한 명세: `docs/v0.1_spec.md` 참조.

```
POST /api/auth/register        가입
POST /api/auth/login           로그인
GET  /api/me                   내 정보
POST /api/devices/register     디바이스 등록
POST /api/friends/add          친구 추가
GET  /api/friends              친구 목록
POST /api/messages             메시지 전송
GET  /api/messages/pending     미수신 메시지
WS   /ws                       WebSocket (실시간)
GET  /health                   헬스체크
```

---

## 모니터링

```bash
pm2 status
pm2 logs noah-server
curl http://localhost:3001/health
```

---

자세한 내용: [`docs/Noah_design.md`](../docs/Noah_design.md), [`docs/v0.1_spec.md`](../docs/v0.1_spec.md)
