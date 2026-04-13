// WebSocket 라우터
// 클라이언트 연결 → 인증 → 메시지 broadcasting

const jwt = require('jsonwebtoken');
const { JWT_SECRET } = require('../routes/auth');
const WebSocket = require('ws');

module.exports = (db, logger, wss) => {
    // device_id → ws 매핑
    const deviceConnections = new Map();
    // user_id → Set<device_id>
    const userDevices = new Map();
    
    function handleConnection(ws, req, aiBot) {
        let authenticated = false;
        let userId = null;
        let deviceId = null;
        
        ws.on('message', async (data) => {
            try {
                const msg = JSON.parse(data.toString());
                
                // 1. 인증
                if (msg.type === 'auth') {
                    try {
                        const decoded = jwt.verify(msg.token, JWT_SECRET);
                        userId = decoded.user_id;
                        deviceId = msg.device_id;
                        
                        // 디바이스 검증
                        const device = db.prepare(`
                            SELECT user_id FROM devices WHERE device_id = ?
                        `).get(deviceId);
                        
                        if (!device || device.user_id !== userId) {
                            ws.send(JSON.stringify({ type: 'auth_error', error: 'invalid device' }));
                            ws.close();
                            return;
                        }
                        
                        authenticated = true;
                        deviceConnections.set(deviceId, ws);
                        
                        if (!userDevices.has(userId)) userDevices.set(userId, new Set());
                        userDevices.get(userId).add(deviceId);
                        
                        // 온라인 표시
                        db.prepare('UPDATE devices SET is_online = 1, last_seen = ? WHERE device_id = ?')
                          .run(Date.now(), deviceId);
                        
                        ws.send(JSON.stringify({ type: 'auth_ok', user_id: userId, device_id: deviceId }));
                        logger.info(`WS authenticated: ${decoded.username} (device: ${deviceId})`);
                        
                        // 큐에 있는 메시지 전송
                        await deliverPending(ws, deviceId);
                    } catch (err) {
                        ws.send(JSON.stringify({ type: 'auth_error', error: err.message }));
                        ws.close();
                    }
                    return;
                }
                
                if (!authenticated) {
                    ws.send(JSON.stringify({ type: 'error', error: 'not authenticated' }));
                    return;
                }
                
                // 2. Ping
                if (msg.type === 'ping') {
                    ws.send(JSON.stringify({ type: 'pong' }));
                    return;
                }
                
                // 3. 메시지 전송
                if (msg.type === 'message') {
                    await handleMessage(msg, userId, deviceId, aiBot);
                    return;
                }
                
                // 4. ACK
                if (msg.type === 'ack') {
                    handleAck(msg, deviceId);
                    return;
                }
            } catch (err) {
                logger.error('WS message error:', err);
                ws.send(JSON.stringify({ type: 'error', error: err.message }));
            }
        });
        
        ws.on('close', () => {
            if (deviceId) {
                deviceConnections.delete(deviceId);
                if (userId && userDevices.has(userId)) {
                    userDevices.get(userId).delete(deviceId);
                }
                
                db.prepare('UPDATE devices SET is_online = 0, last_seen = ? WHERE device_id = ?')
                  .run(Date.now(), deviceId);
                
                logger.info(`WS disconnected: device=${deviceId}`);
            }
        });
        
        ws.on('error', (err) => {
            logger.error('WS error:', err);
        });
    }
    
    async function handleMessage(msg, fromUserId, fromDeviceId, aiBot) {
        const { msg_id, target_user_id, type, payload } = msg;
        
        if (!msg_id || !target_user_id || !type || !payload) {
            return;
        }
        
        // 친구 관계 확인 (AI 봇은 예외)
        const isAiBot = target_user_id === 'ai_crew';
        if (!isAiBot) {
            const friend = db.prepare(`
                SELECT 1 FROM friendships WHERE user_id = ? AND friend_user_id = ?
            `).get(fromUserId, target_user_id);
            
            if (!friend) {
                logger.warn(`Message blocked: ${fromUserId} -> ${target_user_id} (not friends)`);
                return;
            }
        }
        
        // server_seq 발급
        db.prepare('UPDATE seq_counter SET value = value + 1 WHERE name = ?').run('msg_seq');
        const seqRow = db.prepare('SELECT value FROM seq_counter WHERE name = ?').get('msg_seq');
        const serverSeq = seqRow.value;
        
        const now = Date.now();
        const expiresAt = now + 7 * 24 * 60 * 60 * 1000;
        
        // 발신자 정보
        const fromUser = db.prepare('SELECT username, display_name FROM users WHERE user_id = ?').get(fromUserId);
        
        const messageObj = {
            type: 'new_message',
            msg_id,
            from_user_id: fromUserId,
            from_username: fromUser.username,
            from_display_name: fromUser.display_name,
            target_user_id,
            msg_type: type,
            payload,
            server_seq: serverSeq,
            timestamp: now
        };
        
        // 대상 사용자의 모든 디바이스
        const targetDevices = db.prepare('SELECT device_id FROM devices WHERE user_id = ?').all(target_user_id);
        
        // 발신자 자신의 다른 디바이스도 (자기 메시지 동기화)
        const selfDevices = db.prepare('SELECT device_id FROM devices WHERE user_id = ? AND device_id != ?')
                              .all(fromUserId, fromDeviceId);
        
        const allTargets = [...targetDevices, ...selfDevices];
        
        const insertPending = db.prepare(`
            INSERT OR IGNORE INTO pending_messages (
                msg_id, target_device_id, from_user_id, target_user_id,
                type, payload, server_seq, created_at, expires_at
            ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
        `);
        
        for (const dev of allTargets) {
            // 큐에 저장 (오프라인 대비)
            insertPending.run(
                msg_id, dev.device_id, fromUserId, target_user_id,
                type, JSON.stringify(payload), serverSeq, now, expiresAt
            );
            
            // 온라인이면 즉시 전송
            const targetWs = deviceConnections.get(dev.device_id);
            if (targetWs && targetWs.readyState === WebSocket.OPEN) {
                targetWs.send(JSON.stringify(messageObj));
            }
        }
        
        // 발신자에게 확인
        const senderWs = deviceConnections.get(fromDeviceId);
        if (senderWs) {
            senderWs.send(JSON.stringify({
                type: 'message_ack',
                msg_id,
                server_seq: serverSeq,
                server_timestamp: now
            }));
        }
        
        // AI 봇 트리거
        if (isAiBot || (payload.text && payload.text.includes('@크루'))) {
            await aiBot.handleMention(messageObj, fromUserId, target_user_id);
        }
    }
    
    function handleAck(msg, deviceId) {
        const { msg_ids } = msg;
        if (!Array.isArray(msg_ids)) return;
        
        const del = db.prepare(`
            DELETE FROM pending_messages WHERE msg_id = ? AND target_device_id = ?
        `);
        
        const tx = db.transaction(() => {
            for (const msgId of msg_ids) {
                del.run(msgId, deviceId);
            }
        });
        tx();
    }
    
    async function deliverPending(ws, deviceId) {
        const pending = db.prepare(`
            SELECT msg_id, from_user_id, target_user_id, type, payload, server_seq, created_at
            FROM pending_messages
            WHERE target_device_id = ?
            ORDER BY server_seq
        `).all(deviceId);
        
        for (const m of pending) {
            try { m.payload = JSON.parse(m.payload); } catch {}
            
            ws.send(JSON.stringify({
                type: 'new_message',
                msg_id: m.msg_id,
                from_user_id: m.from_user_id,
                target_user_id: m.target_user_id,
                msg_type: m.type,
                payload: m.payload,
                server_seq: m.server_seq,
                timestamp: m.created_at
            }));
        }
        
        if (pending.length > 0) {
            logger.info(`Delivered ${pending.length} pending messages to ${deviceId}`);
        }
    }
    
    // AI 봇이 사용할 broadcast 함수
    function broadcastToUser(userId, message) {
        const devices = userDevices.get(userId);
        if (!devices) return;
        
        for (const deviceId of devices) {
            const ws = deviceConnections.get(deviceId);
            if (ws && ws.readyState === WebSocket.OPEN) {
                ws.send(JSON.stringify(message));
            }
        }
    }
    
    // 그룹 방 멤버 전원에게 broadcast
    function broadcastToRoom(db, roomId, message, excludeUserId) {
        const members = db.prepare(
            'SELECT user_id FROM room_members WHERE room_id = ?'
        ).all(roomId);

        for (const member of members) {
            if (member.user_id === excludeUserId) continue;
            const devices = userDevices.get(member.user_id);
            if (!devices) continue;

            for (const devId of devices) {
                const ws = deviceConnections.get(devId);
                if (ws && ws.readyState === WebSocket.OPEN) {
                    ws.send(JSON.stringify(message));
                }
            }
        }
    }

    return {
        handleConnection,
        broadcastToUser,
        broadcastToRoom,
        deviceConnections,
        userDevices
    };
};
