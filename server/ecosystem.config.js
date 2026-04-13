module.exports = {
    apps: [{
        name: 'noah-server',
        script: 'server.js',
        instances: 1,
        exec_mode: 'fork',
        autorestart: true,
        max_restarts: 100,
        min_uptime: '10s',
        max_memory_restart: '500M',
        watch: false,
        env: {
            NODE_ENV: 'production',
            PORT: 3001
        },
        error_file: 'logs/pm2-error.log',
        out_file: 'logs/pm2-out.log',
        log_date_format: 'YYYY-MM-DD HH:mm:ss',
        merge_logs: true,
        time: true
    }]
};
