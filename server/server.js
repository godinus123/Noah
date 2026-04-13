// NOAH v0.1 — Server
// 비손서버: cd ~/Noah/server && npm install && npm start

require('dotenv').config();
const express = require('express');
const http = require('http');
const WebSocket = require('ws');
const Database = require('better-sqlite3');
const path = require('path');
const fs = require('fs');
const cors = require('cors');
const winston = require('winston');

// === Logger ===
const logger = winston.createLogger({
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

// === DB Init ===
const dataDir = path.join(__dirname, 'data');
const logsDir = path.join(__dirname, 'logs');
const filesDir = path.join(__dirname, 'data', 'files');
[dataDir, logsDir, filesDir].forEach(d => {
    if (!fs.existsSync(d)) fs.mkdirSync(d, { recursive: true });
});

const db = new Database(path.join(dataDir, 'noah_server.db'));
db.pragma('journal_mode = WAL');
db.pragma('foreign_keys = ON');

const schema = fs.readFileSync(path.join(__dirname, 'db', 'schema.sql'), 'utf8');
db.exec(schema);
logger.info('Database initialized');

// === Express App ===
const app = express();
app.use(cors());
app.use(express.json({ limit: '10mb' }));

// 라우터 (별도 파일에서 로드)
app.use('/api/auth', require('./routes/auth')(db, logger));
app.use('/api/me', require('./routes/me')(db, logger));
app.use('/api/devices', require('./routes/devices')(db, logger));
app.use('/api/friends', require('./routes/friends')(db, logger));
app.use('/api/messages', require('./routes/messages')(db, logger));
app.use('/api/files', require('./routes/files')(db, logger, filesDir));

// 헬스체크
app.get('/health', (req, res) => {
    res.json({
        status: 'ok',
        version: '0.1.0',
        uptime: process.uptime(),
        connections: wss?.clients.size || 0
    });
});

// 루트
app.get('/', (req, res) => {
    res.json({
        name: 'NOAH Server',
        version: '0.1.0',
        description: 'Networked Operations Agent Hub'
    });
});

// === HTTP + WebSocket Server ===
const server = http.createServer(app);
const wss = new WebSocket.Server({ server, path: '/ws' });

// WebSocket 라우터 로드
const wsRouter = require('./services/ws_router')(db, logger, wss);
const aiBot = require('./services/ai_bot')(db, logger, wsRouter);

wss.on('connection', (ws, req) => {
    wsRouter.handleConnection(ws, req, aiBot);
});

// === 시작 ===
const PORT = process.env.PORT || 4001;
server.listen(PORT, () => {
    logger.info(`🕊️ NOAH Server v0.1 running on :${PORT}`);
    logger.info(`Health: http://localhost:${PORT}/health`);
    logger.info(`WebSocket: ws://localhost:${PORT}/ws`);
});

// === Graceful shutdown ===
process.on('SIGTERM', () => {
    logger.info('SIGTERM received, shutting down...');
    wss.clients.forEach(client => client.close());
    server.close(() => {
        db.close();
        logger.info('Server closed');
        process.exit(0);
    });
});

// === 에러 핸들링 ===
process.on('uncaughtException', (err) => {
    logger.error('Uncaught exception:', err);
});

process.on('unhandledRejection', (reason, promise) => {
    logger.error('Unhandled rejection:', reason);
});

// === 만료 메시지 정리 (1시간마다) ===
setInterval(() => {
    const now = Date.now();
    const result = db.prepare('DELETE FROM pending_messages WHERE expires_at < ?').run(now);
    if (result.changes > 0) {
        logger.info(`Cleaned up ${result.changes} expired messages`);
    }
    
    const fileResult = db.prepare('DELETE FROM pending_files WHERE expires_at < ?').run(now);
    if (fileResult.changes > 0) {
        logger.info(`Cleaned up ${fileResult.changes} expired files`);
    }
}, 60 * 60 * 1000);
