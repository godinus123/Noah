// 그룹 채팅방 API
const express = require('express');
const { verifyToken } = require('./auth');
const { v4: uuidv4 } = require('uuid');

module.exports = (db, logger) => {
    const router = express.Router();
    router.use(verifyToken);

    // 방 생성
    router.post('/create', (req, res) => {
        try {
            const { room_name, members } = req.body;
            if (!room_name) return res.status(400).json({ error: 'room_name required' });

            const roomId = 'room_' + uuidv4().replace(/-/g, '').substring(0, 16);
            const now = Date.now();

            db.prepare(
                'INSERT INTO rooms (room_id, room_name, room_type, created_by, created_at) VALUES (?, ?, \'group\', ?, ?)'
            ).run(roomId, room_name, req.user.user_id, now);

            // 생성자를 owner로 추가
            db.prepare(
                'INSERT INTO room_members (room_id, user_id, role, joined_at) VALUES (?, ?, \'owner\', ?)'
            ).run(roomId, req.user.user_id, now);

            // 추가 멤버 초대
            if (members && Array.isArray(members)) {
                const insert = db.prepare(
                    'INSERT OR IGNORE INTO room_members (room_id, user_id, role, joined_at) VALUES (?, ?, \'member\', ?)'
                );
                for (const userId of members) {
                    insert.run(roomId, userId, now);
                }
            }

            const memberCount = db.prepare('SELECT COUNT(*) as cnt FROM room_members WHERE room_id = ?').get(roomId).cnt;
            logger.info('Room created: ' + roomId + ' (' + room_name + ') by ' + req.user.username);
            res.json({ room_id: roomId, room_name, member_count: memberCount });
        } catch (err) {
            logger.error('Create room error:', err);
            res.status(500).json({ error: 'internal server error' });
        }
    });

    // 내 방 목록
    router.get('/', (req, res) => {
        try {
            const rooms = db.prepare(
                'SELECT r.room_id, r.room_name, r.room_type, r.created_at, ' +
                '(SELECT COUNT(*) FROM room_members WHERE room_id = r.room_id) as member_count ' +
                'FROM rooms r JOIN room_members rm ON r.room_id = rm.room_id ' +
                'WHERE rm.user_id = ? ORDER BY r.created_at DESC'
            ).all(req.user.user_id);
            res.json(rooms);
        } catch (err) {
            logger.error('List rooms error:', err);
            res.status(500).json({ error: 'internal server error' });
        }
    });

    // 방 정보 + 멤버
    router.get('/:room_id', (req, res) => {
        try {
            const { room_id } = req.params;
            const room = db.prepare('SELECT * FROM rooms WHERE room_id = ?').get(room_id);
            if (!room) return res.status(404).json({ error: 'room not found' });

            const isMember = db.prepare('SELECT 1 FROM room_members WHERE room_id = ? AND user_id = ?')
                .get(room_id, req.user.user_id);
            if (!isMember) return res.status(403).json({ error: 'not a member' });

            const members = db.prepare(
                'SELECT u.user_id, u.username, u.display_name, rm.role, rm.joined_at ' +
                'FROM room_members rm JOIN users u ON rm.user_id = u.user_id ' +
                'WHERE rm.room_id = ? ORDER BY rm.role, u.username'
            ).all(room_id);

            res.json({ ...room, members });
        } catch (err) {
            logger.error('Get room error:', err);
            res.status(500).json({ error: 'internal server error' });
        }
    });

    // 방에 메시지 전송
    router.post('/:room_id/messages', (req, res) => {
        try {
            const { room_id } = req.params;
            const { msg_id, type, payload } = req.body;
            if (!msg_id || !type || !payload) return res.status(400).json({ error: 'msg_id, type, payload required' });

            const isMember = db.prepare('SELECT 1 FROM room_members WHERE room_id = ? AND user_id = ?')
                .get(room_id, req.user.user_id);
            if (!isMember) return res.status(403).json({ error: 'not a member' });

            db.prepare("UPDATE seq_counter SET value = value + 1 WHERE name = 'msg_seq'").run();
            const seqRow = db.prepare("SELECT value FROM seq_counter WHERE name = 'msg_seq'").get();
            const serverSeq = seqRow.value;
            const now = Date.now();

            db.prepare(
                'INSERT INTO room_messages (msg_id, room_id, from_user_id, type, payload, server_seq, created_at) ' +
                'VALUES (?, ?, ?, ?, ?, ?, ?)'
            ).run(msg_id, room_id, req.user.user_id, type, JSON.stringify(payload), serverSeq, now);

            logger.info('Room msg: ' + req.user.username + ' -> ' + room_id);
            res.json({ msg_id, room_id, server_seq: serverSeq, timestamp: now });
        } catch (err) {
            logger.error('Room message error:', err);
            res.status(500).json({ error: 'internal server error' });
        }
    });

    // 방 메시지 히스토리
    router.get('/:room_id/messages', (req, res) => {
        try {
            const { room_id } = req.params;
            const { after_seq, limit } = req.query;

            const isMember = db.prepare('SELECT 1 FROM room_members WHERE room_id = ? AND user_id = ?')
                .get(room_id, req.user.user_id);
            if (!isMember) return res.status(403).json({ error: 'not a member' });

            const msgLimit = Math.min(parseInt(limit) || 50, 200);
            const afterSeq = parseInt(after_seq) || 0;

            const messages = db.prepare(
                'SELECT rm.msg_id, rm.from_user_id, u.username as from_username, ' +
                'u.display_name as from_display_name, rm.type, rm.payload, rm.server_seq, rm.created_at ' +
                'FROM room_messages rm JOIN users u ON rm.from_user_id = u.user_id ' +
                'WHERE rm.room_id = ? AND rm.server_seq > ? ORDER BY rm.server_seq ASC LIMIT ?'
            ).all(room_id, afterSeq, msgLimit);

            res.json(messages.map(m => ({ ...m, payload: JSON.parse(m.payload) })));
        } catch (err) {
            logger.error('Room messages error:', err);
            res.status(500).json({ error: 'internal server error' });
        }
    });

    // 멤버 추가
    router.post('/:room_id/members', (req, res) => {
        try {
            const { room_id } = req.params;
            const { user_id } = req.body;
            if (!user_id) return res.status(400).json({ error: 'user_id required' });

            const myRole = db.prepare('SELECT role FROM room_members WHERE room_id = ? AND user_id = ?')
                .get(room_id, req.user.user_id);
            if (!myRole || (myRole.role !== 'owner' && myRole.role !== 'admin')) {
                return res.status(403).json({ error: 'not authorized' });
            }

            db.prepare(
                'INSERT OR IGNORE INTO room_members (room_id, user_id, role, joined_at) VALUES (?, ?, \'member\', ?)'
            ).run(room_id, user_id, Date.now());

            res.json({ room_id, user_id, status: 'added' });
        } catch (err) {
            logger.error('Add member error:', err);
            res.status(500).json({ error: 'internal server error' });
        }
    });

    return router;
};
