// 메시지 API
const express = require('express');
const { verifyToken } = require('./auth');

const MESSAGE_TTL_MS = 7 * 24 * 60 * 60 * 1000; // 7일

module.exports = (db, logger) => {
    const router = express.Router();
    router.use(verifyToken);
    
    // 메시지 전송 (HTTP 폴백 - 보통은 WebSocket으로)
    router.post('/', (req, res) => {
        try {
            const { msg_id, target_user_id, type, payload } = req.body;
            
            if (!msg_id || !target_user_id || !type || !payload) {
                return res.status(400).json({ error: 'missing fields' });
            }
            
            // 친구 관계 확인
            const friend = db.prepare(`
                SELECT 1 FROM friendships WHERE user_id = ? AND friend_user_id = ?
            `).get(req.user.user_id, target_user_id);
            
            // AI 봇은 친구 아니어도 OK
            const isAiBot = target_user_id === 'ai_crew';
            
            if (!friend && !isAiBot) {
                return res.status(403).json({ error: 'not friends' });
            }
            
            // server_seq 발급
            db.prepare('UPDATE seq_counter SET value = value + 1 WHERE name = ?').run('msg_seq');
            const seqRow = db.prepare('SELECT value FROM seq_counter WHERE name = ?').get('msg_seq');
            const serverSeq = seqRow.value;
            
            const now = Date.now();
            const expiresAt = now + MESSAGE_TTL_MS;
            
            // 대상 사용자의 모든 디바이스에 큐
            const targetDevices = db.prepare('SELECT device_id FROM devices WHERE user_id = ?').all(target_user_id);
            
            const insert = db.prepare(`
                INSERT OR IGNORE INTO pending_messages (
                    msg_id, target_device_id, from_user_id, target_user_id,
                    type, payload, server_seq, created_at, expires_at
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
            `);
            
            for (const dev of targetDevices) {
                insert.run(
                    msg_id, dev.device_id, req.user.user_id, target_user_id,
                    type, JSON.stringify(payload), serverSeq, now, expiresAt
                );
            }
            
            res.json({
                msg_id,
                server_seq: serverSeq,
                server_timestamp: now,
                queued_devices: targetDevices.length
            });
        } catch (err) {
            logger.error('Send message error:', err);
            res.status(500).json({ error: 'internal server error' });
        }
    });
    
    // 미수신 메시지 조회
    router.get('/pending', (req, res) => {
        try {
            const deviceId = req.headers['device-id'];
            if (!deviceId) return res.status(400).json({ error: 'device-id header required' });
            
            // 디바이스 소유자 확인
            const device = db.prepare(`
                SELECT user_id FROM devices WHERE device_id = ?
            `).get(deviceId);
            
            if (!device || device.user_id !== req.user.user_id) {
                return res.status(403).json({ error: 'device not yours' });
            }
            
            const messages = db.prepare(`
                SELECT msg_id, from_user_id, target_user_id, type, payload, server_seq, created_at
                FROM pending_messages
                WHERE target_device_id = ?
                ORDER BY server_seq
            `).all(deviceId);
            
            // payload는 JSON 문자열이므로 파싱
            for (const m of messages) {
                try { m.payload = JSON.parse(m.payload); } catch {}
            }
            
            res.json(messages);
        } catch (err) {
            logger.error('Get pending error:', err);
            res.status(500).json({ error: 'internal server error' });
        }
    });
    
    // 메시지 ACK (수신 확인 → 큐에서 삭제)
    router.post('/ack', (req, res) => {
        try {
            const { msg_ids } = req.body;
            const deviceId = req.headers['device-id'];
            
            if (!Array.isArray(msg_ids) || !deviceId) {
                return res.status(400).json({ error: 'msg_ids array and device-id required' });
            }
            
            const del = db.prepare(`
                DELETE FROM pending_messages WHERE msg_id = ? AND target_device_id = ?
            `);
            
            let acked = 0;
            const tx = db.transaction(() => {
                for (const msgId of msg_ids) {
                    const r = del.run(msgId, deviceId);
                    acked += r.changes;
                }
            });
            tx();
            
            res.json({ acked });
        } catch (err) {
            logger.error('ACK error:', err);
            res.status(500).json({ error: 'internal server error' });
        }
    });
    
    return router;
};
