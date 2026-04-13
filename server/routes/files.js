// 첨부 파일 API
const express = require('express');
const multer = require('multer');
const path = require('path');
const fs = require('fs');
const { v4: uuidv4 } = require('uuid');
const { verifyToken } = require('./auth');

const FILE_TTL_MS = 7 * 24 * 60 * 60 * 1000;

module.exports = (db, logger, filesDir) => {
    const router = express.Router();
    
    const upload = multer({
        dest: filesDir,
        limits: { fileSize: 50 * 1024 * 1024 } // 50MB
    });
    
    // 파일 업로드
    router.post('/upload', verifyToken, upload.single('file'), (req, res) => {
        try {
            if (!req.file) return res.status(400).json({ error: 'no file' });
            
            const fileId = `file_${uuidv4().replace(/-/g, '').substring(0, 16)}`;
            const now = Date.now();
            
            db.prepare(`
                INSERT INTO pending_files (
                    file_id, msg_id, from_user_id, filename, mime, size,
                    storage_path, created_at, expires_at
                ) VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?)
            `).run(
                fileId, null, req.user.user_id,
                req.file.originalname, req.file.mimetype, req.file.size,
                req.file.path, now, now + FILE_TTL_MS
            );
            
            logger.info(`File uploaded: ${req.file.originalname} (${fileId}) by ${req.user.username}`);
            
            res.json({
                file_id: fileId,
                filename: req.file.originalname,
                mime: req.file.mimetype,
                size: req.file.size,
                expires_at: now + FILE_TTL_MS
            });
        } catch (err) {
            logger.error('Upload error:', err);
            res.status(500).json({ error: 'internal server error' });
        }
    });
    
    // 파일 다운로드
    router.get('/:file_id', verifyToken, (req, res) => {
        try {
            const file = db.prepare(`
                SELECT * FROM pending_files WHERE file_id = ?
            `).get(req.params.file_id);
            
            if (!file) return res.status(404).json({ error: 'file not found' });
            if (file.expires_at < Date.now()) return res.status(410).json({ error: 'file expired' });
            if (!fs.existsSync(file.storage_path)) {
                return res.status(404).json({ error: 'file missing' });
            }
            
            res.setHeader('Content-Type', file.mime || 'application/octet-stream');
            res.setHeader('Content-Disposition', `attachment; filename="${file.filename}"`);
            res.sendFile(path.resolve(file.storage_path));
        } catch (err) {
            logger.error('Download error:', err);
            res.status(500).json({ error: 'internal server error' });
        }
    });
    
    return router;
};
