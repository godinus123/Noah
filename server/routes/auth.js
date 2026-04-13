// 가입/로그인 API
const express = require('express');
const bcrypt = require('bcrypt');
const jwt = require('jsonwebtoken');
const { v4: uuidv4 } = require('uuid');

const JWT_SECRET = process.env.JWT_SECRET || 'noah-dev-secret-change-me';
const JWT_EXPIRES = '30d';

module.exports = (db, logger) => {
    const router = express.Router();
    
    // 가입
    router.post('/register', async (req, res) => {
        try {
            const { username, password, display_name } = req.body;
            
            if (!username || !password) {
                return res.status(400).json({ error: 'username and password required' });
            }
            
            if (username.length < 2 || username.length > 30) {
                return res.status(400).json({ error: 'username must be 2-30 chars' });
            }
            
            if (password.length < 4) {
                return res.status(400).json({ error: 'password must be 4+ chars' });
            }
            
            // 중복 체크
            const exists = db.prepare('SELECT user_id FROM users WHERE username = ?').get(username);
            if (exists) {
                return res.status(409).json({ error: 'username already taken' });
            }
            
            // 비밀번호 해시
            const hash = await bcrypt.hash(password, 10);
            
            // 사용자 생성
            const userId = `user_${uuidv4().replace(/-/g, '').substring(0, 16)}`;
            const now = Date.now();
            
            db.prepare(`
                INSERT INTO users (user_id, username, password_hash, display_name, created_at, last_seen)
                VALUES (?, ?, ?, ?, ?, ?)
            `).run(userId, username, hash, display_name || username, now, now);
            
            // 토큰 발급
            const token = jwt.sign({ user_id: userId, username }, JWT_SECRET, { expiresIn: JWT_EXPIRES });
            
            logger.info(`New user registered: ${username} (${userId})`);
            
            res.json({
                user_id: userId,
                username,
                display_name: display_name || username,
                token
            });
        } catch (err) {
            logger.error('Register error:', err);
            res.status(500).json({ error: 'internal server error' });
        }
    });
    
    // 로그인
    router.post('/login', async (req, res) => {
        try {
            const { username, password } = req.body;
            
            if (!username || !password) {
                return res.status(400).json({ error: 'username and password required' });
            }
            
            const user = db.prepare('SELECT * FROM users WHERE username = ?').get(username);
            if (!user) {
                return res.status(401).json({ error: 'invalid credentials' });
            }
            
            const valid = await bcrypt.compare(password, user.password_hash);
            if (!valid) {
                return res.status(401).json({ error: 'invalid credentials' });
            }
            
            // 토큰 발급
            const token = jwt.sign(
                { user_id: user.user_id, username: user.username },
                JWT_SECRET,
                { expiresIn: JWT_EXPIRES }
            );
            
            // last_seen 업데이트
            db.prepare('UPDATE users SET last_seen = ? WHERE user_id = ?').run(Date.now(), user.user_id);
            
            logger.info(`User logged in: ${username}`);
            
            res.json({
                user_id: user.user_id,
                username: user.username,
                display_name: user.display_name,
                avatar_url: user.avatar_url,
                status_message: user.status_message,
                token
            });
        } catch (err) {
            logger.error('Login error:', err);
            res.status(500).json({ error: 'internal server error' });
        }
    });
    
    // 토큰 검증 (다른 라우트에서 사용)
    router.verifyToken = (req, res, next) => {
        const auth = req.headers.authorization;
        if (!auth || !auth.startsWith('Bearer ')) {
            return res.status(401).json({ error: 'no token' });
        }
        
        try {
            const token = auth.substring(7);
            const decoded = jwt.verify(token, JWT_SECRET);
            req.user = decoded;
            next();
        } catch (err) {
            return res.status(401).json({ error: 'invalid token' });
        }
    };
    
    return router;
};

// 다른 라우트에서 토큰 검증할 때 쓸 수 있게 export
module.exports.verifyToken = (req, res, next) => {
    const auth = req.headers.authorization;
    if (!auth || !auth.startsWith('Bearer ')) {
        return res.status(401).json({ error: 'no token' });
    }
    
    try {
        const token = auth.substring(7);
        const decoded = jwt.verify(token, JWT_SECRET);
        req.user = decoded;
        next();
    } catch (err) {
        return res.status(401).json({ error: 'invalid token' });
    }
};

module.exports.JWT_SECRET = JWT_SECRET;
