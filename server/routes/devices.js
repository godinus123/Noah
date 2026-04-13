// 디바이스 관리 API
const express = require('express');
const { v4: uuidv4 } = require('uuid');
const { verifyToken } = require('./auth');

module.exports = (db, logger) => {
    const router = express.Router();
    router.use(verifyToken);
    
    // 디바이스 등록
    router.post('/register', (req, res) => {
        try {
            const { device_name, device_type } = req.body;
            
            const deviceId = `dev_${uuidv4().replace(/-/g, '').substring(0, 16)}`;
            const now = Date.now();
            
            db.prepare(`
                INSERT INTO devices (device_id, user_id, device_name, device_type, last_seen, is_online, created_at)
                VALUES (?, ?, ?, ?, ?, 1, ?)
            `).run(deviceId, req.user.user_id, device_name || 'Unknown', device_type || 'unknown', now, now);
            
            logger.info(`Device registered: ${device_name} (${deviceId}) for ${req.user.username}`);
            res.json({ device_id: deviceId });
        } catch (err) {
            logger.error('Register device error:', err);
            res.status(500).json({ error: 'internal server error' });
        }
    });
    
    // 내 디바이스 목록
    router.get('/', (req, res) => {
        try {
            const devices = db.prepare(`
                SELECT device_id, device_name, device_type, last_seen, is_online, created_at
                FROM devices WHERE user_id = ?
                ORDER BY last_seen DESC
            `).all(req.user.user_id);
            
            res.json(devices);
        } catch (err) {
            logger.error('List devices error:', err);
            res.status(500).json({ error: 'internal server error' });
        }
    });
    
    // 디바이스 삭제 (원격 로그아웃)
    router.delete('/:device_id', (req, res) => {
        try {
            const result = db.prepare(`
                DELETE FROM devices WHERE device_id = ? AND user_id = ?
            `).run(req.params.device_id, req.user.user_id);
            
            res.json({ ok: result.changes > 0 });
        } catch (err) {
            logger.error('Delete device error:', err);
            res.status(500).json({ error: 'internal server error' });
        }
    });
    
    return router;
};
