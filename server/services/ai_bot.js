// AI 봇 (@크루)
// 사용자가 @크루 멘션 시 Claude API 호출 후 응답

const Anthropic = require('@anthropic-ai/sdk');
const { v4: uuidv4 } = require('uuid');

module.exports = (db, logger, wsRouter) => {
    let claude = null;
    
    try {
        if (process.env.ANTHROPIC_API_KEY) {
            claude = new Anthropic({
                apiKey: process.env.ANTHROPIC_API_KEY,
                baseURL: process.env.ANTHROPIC_BASE_URL || undefined
            });
            logger.info('AI bot initialized');
        } else {
            logger.warn('ANTHROPIC_API_KEY not set, AI bot disabled');
        }
    } catch (err) {
        logger.error('AI bot init error:', err);
    }
    
    const SYSTEM_PROMPT = `당신은 NOAH 메신저의 AI 어시스턴트 '크루'입니다.

NOAH는 사람과 AI 에이전트가 함께 사용하는 P2P 메신저입니다.
NOAH = Networked Operations Agent Hub.

당신의 역할:
- 사용자의 질문에 친근하게 답변
- 코드 작성, 디버깅, 설명 도움
- 마크다운 형식으로 응답 (코드 블록 포함)
- 한국어로 자연스럽게 응답

답변은 간결하고 정확하게.`;
    
    async function handleMention(messageObj, fromUserId, targetUserId) {
        if (!claude) {
            sendBotMessage(fromUserId, '⚠️ AI 봇이 비활성화되어 있습니다. 서버 관리자에게 ANTHROPIC_API_KEY 설정을 요청하세요.');
            return;
        }
        
        try {
            const text = messageObj.payload.text || '';
            // @크루 제거
            const question = text.replace(/@크루|@crew/gi, '').trim() || '안녕하세요';
            
            logger.info(`AI bot triggered by ${messageObj.from_username}: ${question.substring(0, 50)}...`);
            
            // Claude API 호출
            const response = await claude.messages.create({
                model: process.env.AI_MODEL || 'claude-sonnet-4-6',
                max_tokens: 2000,
                system: SYSTEM_PROMPT,
                messages: [
                    { role: 'user', content: question }
                ]
            });
            
            const aiText = response.content[0].text;
            
            // AI 응답을 메시지로 broadcast
            sendBotMessage(fromUserId, aiText);
            
            logger.info(`AI bot responded: ${aiText.substring(0, 50)}...`);
        } catch (err) {
            logger.error('AI bot error:', err);
            sendBotMessage(fromUserId, `⚠️ AI 응답 오류: ${err.message}`);
        }
    }
    
    function sendBotMessage(targetUserId, text) {
        const msgId = `msg_${uuidv4().replace(/-/g, '').substring(0, 16)}`;
        
        // server_seq
        db.prepare('UPDATE seq_counter SET value = value + 1 WHERE name = ?').run('msg_seq');
        const seqRow = db.prepare('SELECT value FROM seq_counter WHERE name = ?').get('msg_seq');
        const serverSeq = seqRow.value;
        
        const now = Date.now();
        const expiresAt = now + 7 * 24 * 60 * 60 * 1000;
        
        const messageObj = {
            type: 'new_message',
            msg_id: msgId,
            from_user_id: 'ai_crew',
            from_username: '크루',
            from_display_name: '🕊️ 크루',
            target_user_id: targetUserId,
            msg_type: 'text',
            payload: { text, is_ai: true },
            server_seq: serverSeq,
            timestamp: now,
            is_ai: true
        };
        
        // 큐에 저장 (오프라인 디바이스용)
        const targetDevices = db.prepare('SELECT device_id FROM devices WHERE user_id = ?').all(targetUserId);
        const insert = db.prepare(`
            INSERT OR IGNORE INTO pending_messages (
                msg_id, target_device_id, from_user_id, target_user_id,
                type, payload, server_seq, created_at, expires_at
            ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
        `);
        
        for (const dev of targetDevices) {
            insert.run(
                msgId, dev.device_id, 'ai_crew', targetUserId,
                'text', JSON.stringify({ text, is_ai: true }),
                serverSeq, now, expiresAt
            );
        }
        
        // WebSocket으로 즉시 전송
        wsRouter.broadcastToUser(targetUserId, messageObj);
    }
    
    return {
        handleMention,
        sendBotMessage,
        isEnabled: () => claude !== null
    };
};
