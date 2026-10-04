// js/signalr.js

import { state } from './state.js';
import { playSystemNotificationSound, formatTime } from './utils.js';
import { SIGNALR_HUB } from './config.js';

// Callback-и, которые UI-модули будут регистрировать
const handlers = {
    onReceiveMessage: null,
    onUserStatusChanged: null,
    onUserIsTyping: null,
    onMessagesMarkedAsRead: null,
    onChatInvitation: null,
    onChatDeleted: null,
    onUserLeave: null
};

export function registerHandlers(callbacks) {
    Object.assign(handlers, callbacks);
}

/**
 * Подключение к SignalR хабу
 */
export async function connect() {
    if (state.connection && state.connection.state === signalR.HubConnectionState.Connected) {
        return state.connection;
    }

    state.connection = new signalR.HubConnectionBuilder()
        .withUrl(SIGNALR_HUB, { accessTokenFactory: () => state.jwtToken })
        .withAutomaticReconnect()
        .build();

    registerServerEvents();

    await state.connection.start();
    console.log('[SignalR] Успешное защищенное подключение');
    return state.connection;
}

export async function disconnect() {
    if (state.connection) {
        try { await state.connection.stop(); } catch (e) { console.warn(e); }
        state.connection = null;
    }
}

function registerServerEvents() {
    const conn = state.connection;

    conn.on('ReceiveMessage', (message) => {
        if (handlers.onReceiveMessage) handlers.onReceiveMessage(message);
    });

    conn.on('UserStatusChanged', (data) => {
        if (handlers.onUserStatusChanged) handlers.onUserStatusChanged(data);
    });

    conn.on('UserIsTyping', (data) => {
        if (handlers.onUserIsTyping) handlers.onUserIsTyping(data);
    });

    conn.on('MessagesMarkedAsRead', (data) => {
        if (handlers.onMessagesMarkedAsRead) handlers.onMessagesMarkedAsRead(data);
    });

    conn.on('ChatInvitation', (chat) => {
        if (handlers.onChatInvitation) handlers.onChatInvitation(chat);
    });

    conn.on('ChatDeleted', (data) => {
        if (handlers.onChatDeleted) handlers.onChatDeleted(data);
    });

    conn.on('UserLeave', (data) => {
        if (handlers.onUserLeave) handlers.onUserLeave(data);
    });
}

// ==================== ИСХОДЯЩИЕ ВЫЗОВЫ ====================

export const hub = {
    sendMessage: (chatId, text) =>
        state.connection.invoke('SendMessage', chatId, text),

    joinChat: (chatId) =>
        state.connection.invoke('JoinChat', chatId),

    leaveChat: (chatId) =>
        state.connection.invoke('LeaveChat', chatId),

    notifyTyping: (chatId) =>
        state.connection.invoke('NotifyTyping', chatId),

    markChatAsRead: (chatId) =>
        state.connection.invoke('MarkChatAsRead', chatId)
};