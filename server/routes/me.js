// 프로필 관리 API
const express = require('express');
const bcrypt = require('bcrypt');
const { verifyToken } = require('./auth');

module.exports = (db, logger) => {
    const router = express.Router();
    
    // 모든 라우트에 인증 적용
    router.use(verifyToken);
    
    // 내 정보
    router.get('/', (req, res) => {
        try {
            const user = db.prepare(`
                SELECT user_id, username, display_name, avatar_url, status_message, created_at
                FROM users WHERE user_id = ?
            `).get(req.user.user_id);
            
            if (!user) return res.status(404).json({ error: 'user not found' });
            res.json(user);
        } catch (err) {
            logger.error('Get me error:', err);
            res.status(500).json({ error: 'internal server error' });
        }
    });
    
    // 프로필 업데이트
    router.put('/', (req, res) => {
        try {
            const { display_name, avatar_url, status_message } = req.body;
            
            const fields = [];
            const values = [];
            
            if (display_name !== undefined) { fields.push('display_name = ?'); values.push(display_name); }
            if (avatar_url !== undefined) { fields.push('avatar_url = ?'); values.push(avatar_url); }
            if (status_message !== undefined) { fields.push('status_message = ?'); values.push(status_message); }
            
            if (fields.length === 0) {
                return res.status(400).json({ error: 'no fields to update' });
            }
            
            values.push(req.user.user_id);
            db.prepare(`UPDATE users SET ${fields.join(', ')} WHERE user_id = ?`).run(...values);
            
            res.json({ ok: true });
        } catch (err) {
            logger.error('Update me error:', err);
            res.status(500).json({ error: 'internal server error' });
        }
    });
    
    // 비밀번호 변경
    router.post('/password', async (req, res) => {
        try {
            const { old_password, new_password } = req.body;
            
            if (!old_password || !new_password) {
                return res.status(400).json({ error: 'old and new password required' });
            }
            
            if (new_password.length < 4) {
                return res.status(400).json({ error: 'new password must be 4+ chars' });
            }
            
            const user = db.prepare('SELECT password_hash FROM users WHERE user_id = ?').get(req.user.user_id);
            if (!user) return res.status(404).json({ error: 'user not found' });
            
            const valid = await bcrypt.compare(old_password, user.password_hash);
            if (!valid) return res.status(401).json({ error: 'invalid old password' });
            
            const newHash = await bcrypt.hash(new_password, 10);
            db.prepare('UPDATE users SET password_hash = ? WHERE user_id = ?').run(newHash, req.user.user_id);
            
            logger.info(`Password changed for user: ${req.user.username}`);
            res.json({ ok: true });
        } catch (err) {
            logger.error('Password change error:', err);
            res.status(500).json({ error: 'internal server error' });
        }
    });
    
    return router;
};
