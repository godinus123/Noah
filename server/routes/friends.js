// 친구 관리 API
const express = require('express');
const { verifyToken } = require('./auth');

module.exports = (db, logger) => {
    const router = express.Router();
    router.use(verifyToken);
    
    // 친구 추가 (사용자 이름으로)
    router.post('/add', (req, res) => {
        try {
            const { username } = req.body;
            if (!username) return res.status(400).json({ error: 'username required' });
            
            const friend = db.prepare(`
                SELECT user_id, username, display_name, avatar_url, status_message
                FROM users WHERE username = ?
            `).get(username);
            
            if (!friend) return res.status(404).json({ error: 'user not found' });
            if (friend.user_id === req.user.user_id) {
                return res.status(400).json({ error: 'cannot add yourself' });
            }
            
            // 양방향 친구 관계 (단순화)
            const now = Date.now();
            const insert = db.prepare(`
                INSERT OR IGNORE INTO friendships (user_id, friend_user_id, created_at)
                VALUES (?, ?, ?)
            `);
            insert.run(req.user.user_id, friend.user_id, now);
            insert.run(friend.user_id, req.user.user_id, now);
            
            logger.info(`Friend added: ${req.user.username} <-> ${friend.username}`);
            res.json(friend);
        } catch (err) {
            logger.error('Add friend error:', err);
            res.status(500).json({ error: 'internal server error' });
        }
    });
    
    // 친구 목록
    router.get('/', (req, res) => {
        try {
            const friends = db.prepare(`
                SELECT u.user_id, u.username, u.display_name, u.avatar_url, 
                       u.status_message, u.last_seen
                FROM friendships f
                JOIN users u ON u.user_id = f.friend_user_id
                WHERE f.user_id = ?
                ORDER BY u.username
            `).all(req.user.user_id);
            
            res.json(friends);
        } catch (err) {
            logger.error('List friends error:', err);
            res.status(500).json({ error: 'internal server error' });
        }
    });
    
    // 친구 삭제
    router.delete('/:user_id', (req, res) => {
        try {
            const { user_id } = req.params;
            
            const del = db.prepare(`
                DELETE FROM friendships 
                WHERE (user_id = ? AND friend_user_id = ?) 
                   OR (user_id = ? AND friend_user_id = ?)
            `);
            del.run(req.user.user_id, user_id, user_id, req.user.user_id);
            
            res.json({ ok: true });
        } catch (err) {
            logger.error('Delete friend error:', err);
            res.status(500).json({ error: 'internal server error' });
        }
    });
    
    return router;
};
