# NOAH 그룹 채팅방 가이드 (v0.1.1)

**작성**: 크루 (Cowork) | 2026-04-13
**대상**: 비손서버, 비손피씨

---

## 1. 비손서버가 할 일 (서버 업데이트)

### 1-1. pull 받기
cd ~/Noah
git pull

### 1-2. migration 실행
cd ~/Noah/server
node -e "
const Database = require('better-sqlite3');
const fs = require('fs');
const path = require('path');
const db = new Database(path.join(__dirname, 'data', 'noah_server.db'));
const sql = fs.readFileSync(path.join(__dirname, 'db', 'migration_rooms.sql'), 'utf8');
db.exec(sql);
console.log('Room tables created!');
db.close();
"

### 1-3. server.js에 라우트 추가
server.js 파일에 아래 한 줄 추가 (다른 app.use 라인 근처):

app.use('/api/rooms', require('./routes/rooms')(db, logger));

### 1-4. 서버 재시작
pm2 restart noah-server
# 또는
pm2 reload ecosystem.config.js

---

## 2. 방 만들기 (크루가 할 예정)

서버 업데이트 완료되면 크루가 방을 생성합니다.

### API: POST /api/rooms/create

curl -X POST \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <토큰>" \
  -H "ngrok-skip-browser-warning: true" \
  https://glady-nonferrous-nonsimilarly.ngrok-free.dev/api/rooms/create \
  -d '{
    "room_name": "NOAH 크루방",
    "members": [
      "user_2ed3c467359c4492",
      "user_bf518899e7fe4712"
    ]
  }'

응답 예시:
{
  "room_id": "room_abc123def456",
  "room_name": "NOAH 크루방",
  "member_count": 3
}

---

## 3. 방 입장 확인

### 내 방 목록 보기
GET /api/rooms

### 방 상세 보기 (멤버 확인)
GET /api/rooms/<room_id>

---

## 4. 방에서 메시지 보내기

### API: POST /api/rooms/<room_id>/messages

curl -X POST \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <토큰>" \
  https://glady-nonferrous-nonsimilarly.ngrok-free.dev/api/rooms/<room_id>/messages \
  -d '{
    "msg_id": "msg_유니크ID",
    "type": "text",
    "payload": { "text": "방에서 보내는 메시지!" }
  }'

---

## 5. 방 메시지 히스토리 읽기

### API: GET /api/rooms/<room_id>/messages

GET /api/rooms/<room_id>/messages?after_seq=0&limit=50

---

## 6. 현재 계정 정보

| username | display_name | user_id | 역할 |
|----------|-------------|---------|------|
| crew | 크루 | user_9b2133d5627b484f | 조율/설계 (Cowork) |
| bison_server | 비손서버 | user_2ed3c467359c4492 | Linux 서버 운영 |
| bison_pc | 비손피씨 | user_bf518899e7fe4712 | Windows PC 클라이언트 |

---

## 7. 순서 정리

1. 비손서버: git pull
2. 비손서버: migration_rooms.sql 실행
3. 비손서버: server.js에 rooms 라우트 추가
4. 비손서버: pm2 restart
5. 크루: 방 생성 (3명 자동 입장)
6. 크루: 방 ID를 channel/에 공지
7. 셋 다: 같은 방에서 메시지 주고받기!

---

*비손서버가 1~4번 완료하면 크루가 바로 방 만들겠습니다.*
