/*
 * Клиент для REST API с автообновлением токена (доп. задание — сложный уровень).
 * Использование: открыть любую страницу сайта, DevTools → Console, вставить весь файл, затем:
 *
 *   await api.login('admin@todo.com', 'Admin123!');
 *   await (await api.fetch('/api/tasks')).json();
 *   await api.logoutAll();
 *
 * При 401 клиент сам пробует обновить токен через /api/auth/refresh.
 * Если refresh не сработал — редирект на страницу логина.
 */
window.api = {
    get access() { return sessionStorage.getItem('access'); },
    get refreshToken() { return sessionStorage.getItem('refresh'); },

    _save(data) {
        sessionStorage.setItem('access', data.accessToken);
        sessionStorage.setItem('refresh', data.refreshToken);
    },

    async login(email, password, twoFactorCode) {
        const r = await fetch('/api/auth/login', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ email, password, twoFactorCode })
        });
        const data = await r.json();
        if (r.ok) this._save(data);
        return { ok: r.ok, status: r.status, data };
    },

    async refresh() {
        if (!this.refreshToken) return false;
        const r = await fetch('/api/auth/refresh', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ refreshToken: this.refreshToken })
        });
        if (!r.ok) return false;
        this._save(await r.json());
        return true;
    },

    _send(url, options) {
        const headers = Object.assign({ 'Content-Type': 'application/json' }, options.headers || {});
        if (this.access) headers['Authorization'] = 'Bearer ' + this.access;
        return fetch(url, Object.assign({}, options, { headers }));
    },

    async fetch(url, options = {}) {
        let res = await this._send(url, options);
        if (res.status === 401) {
            console.warn('401 → пробую обновить токен через refresh…');
            if (await this.refresh()) {
                res = await this._send(url, options);
            } else {
                console.warn('Refresh не сработал → переход на страницу входа');
                sessionStorage.clear();
                location.href = '/Account/Login';
            }
        }
        return res;
    },

    async logout() {
        const r = await fetch('/api/auth/logout', {
            method: 'POST',
            headers: { 'Content-Type': 'application/json' },
            body: JSON.stringify({ refreshToken: this.refreshToken })
        });
        sessionStorage.clear();
        return r.json();
    },

    async logoutAll() {
        const r = await this.fetch('/api/auth/logout-all', { method: 'POST' });
        sessionStorage.clear();
        return r.json();
    }
};
console.log('api готов: api.login(email, password), api.fetch(url), api.logoutAll()');
