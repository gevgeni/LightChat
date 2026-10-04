import { state } from './state.js';

async function request(method, url, body = null) {
    const headers = {
        'Authorization': `Bearer ${state.jwtToken}`
    };

    if (body !== null) {
        headers['Content-Type'] = 'application/json';
    }

    const response = await fetch(url, {
        method,
        headers,
        body: body !== null ? JSON.stringify(body) : undefined
    });

    if (response.status === 401) {
        throw new UnauthorizedError();
    }

    if (!response.ok) {
        const test = await response.text();
        throw new ApiError(text || `HTTP ${response.status}`, response.status);
    }

    const contentType = response.headers.get('content-type');
    if (contentType && contentType.includes('application/json')) {
        return response.json();
    }
    return response.text();
}

export class ApiError extends Error {
    constructor(message, status) {
        super(message);
        this.name = 'ApiError';
        this.status = status;
    }
}

export class UnauthorizedError extends Error {
    constructor() {
        super('Unauthorized');
        this.name = 'UnauthorizedError';
    }
}

// ==================== AUTH ====================

export const authApi = {
    login: (username, password) =>
        request('POST', '/auth/login', { username, password }),

    register: (username, email, password) =>
        request('POST', '/users', { username, email, password })
};

// ==================== CHATS ====================

export const chatsApi = {
    getMyChats: () =>
        request('GET', '/chats'),

    create: (name) =>
        request('POST', '/chats', { name }),

    createDirect: (targetUserId) =>
        request('POST', '/chats/direct', { targetUserId }),

    getMembers: (chatId) =>
        request('GET', `/chats/${chatId}/members`),

    addMember: (chatId, userId) =>
        request('POST', `/chats/${chatId}/members`, { userId }),

    leave: (chatId) =>
        request('DELETE', `/chats/${chatId}/leave`),

    delete: (chatId) =>
        request('DELETE', `/chats/${chatId}`)
};

// ==================== MESSAGES ====================

export const messagesApi = {
    getHistory: (chatId, limit, beforeMessageId = null) => {
        let url = `/chats/${chatId}/messages?limit=${limit}`;
        if (beforeMessageId) url += `&beforeMessageId=${beforeMessageId}`;
        return request('GET', url);
    }
};

// ==================== USERS ====================

export const usersApi = {
    getAll: () =>
        request('GET', '/users')
};