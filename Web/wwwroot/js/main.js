// js/main.js

// ==================== ИМПОРТЫ ====================

import { state } from './state.js';
import { connect } from './signalr.js';
import { registerHandlers } from './signalr.js';

import { loginAndLoadApp, registerAccount, logout, toggleAuthMode, initializeFromSavedToken, registerInitCallback } from './auth.js';

import { loadUserChats, selectChat, handleChatInvitation, handleChatDeleted, handleUserLeave } from './ui/chats.js';
import { loadMoreMessages, sendMessage, handleReceiveMessage, handleMessagesMarkedAsRead, handleUserIsTyping, initializeMessagesHandlers } from './ui/messages.js';
import { loadChatMembers, handleUserStatusChanged } from './ui/members.js';
import { toggleMobileSidebar, initializeMobileHandlers } from './ui/mobile.js';
import {
    openCreateChatModal, closeCreateChatModal, submitCreateChat,
    openDirectChatModal, closeDirectChatModal, submitStartDirectChat,
    openAddMemberModal, closeAddMemberModal, submitAddMember,
    leaveChat, deleteChat
} from './ui/modals.js';

// ==================== ИНИЦИАЛИЗАЦИЯ ПРИЛОЖЕНИЯ ====================

/**
 * Инициализация после успешного логина.
 * Подключает SignalR, регистрирует handler-ы, загружает чаты.
 */
async function initializeApp() {
    try {
        // 1. Подключаем SignalR
        await connect();

        // 2. Регистрируем серверные handler-ы
        registerHandlers({
            onReceiveMessage: handleReceiveMessage,
            onUserStatusChanged: handleUserStatusChanged,
            onMessagesMarkedAsRead: handleMessagesMarkedAsRead,
            onChatInvitation: handleChatInvitation,
            onChatDeleted: handleChatDeleted,
            onUserLeave: handleUserLeave,
            onUserIsTyping: handleUserIsTyping
        });

        // 3. Показываем основной интерфейс
        document.getElementById("authSection").style.display = "none";
        document.getElementById("mainInterface").style.display = "block";

        // 4. DOM-обработчики (скролл, overlay-и)
        initializeMobileHandlers();
        initializeMessagesHandlers();

        // 5. Загружаем список чатов
        await loadUserChats();

        console.log("[App] Инициализация завершена");
    } catch (err) {
        console.error("[App] Ошибка инициализации:", err);
        alert("Ошибка: " + err.message);
    }
}

// Регистрируем callback в auth.js
registerInitCallback(initializeApp);

// ==================== СТАРТ ПРИЛОЖЕНИЯ ====================

window.addEventListener('DOMContentLoaded', async () => {
    const restored = await initializeFromSavedToken();
    if (!restored) {
        console.log("[App] Нет активной сессии, показываем форму логина");
    }
});

// ==================== ЭКСПОРТ В window (ВРЕМЕННО) ====================
// Нужно для совместимости с inline onclick="..." в index.html.
// В следующей итерации (0.6.7) заменим inline-обработчики на addEventListener.

window.loginAndLoadApp = loginAndLoadApp;
window.registerAccount = registerAccount;
window.logout = logout;
window.toggleAuthMode = toggleAuthMode;

window.selectChat = selectChat;
window.loadUserChats = loadUserChats;

window.sendMessage = sendMessage;

window.toggleMobileSidebar = toggleMobileSidebar;

window.openCreateChatModal = openCreateChatModal;
window.closeCreateChatModal = closeCreateChatModal;
window.submitCreateChat = submitCreateChat;
window.openDirectChatModal = openDirectChatModal;
window.closeDirectChatModal = closeDirectChatModal;
window.submitStartDirectChat = submitStartDirectChat;
window.openAddMemberModal = openAddMemberModal;
window.closeAddMemberModal = closeAddMemberModal;
window.submitAddMember = submitAddMember;
window.leaveChat = leaveChat;
window.deleteChat = deleteChat;

document.addEventListener('click', () => {
    try {
        const ctx = new (window.AudioContext || window.webkitAudioContext)();
        ctx.resume().then(() => ctx.close());
        console.log('[Audio] AudioContext разблокирован');
    } catch (e) {
        console.warn('[Audio] Ошибка разблокировки:', e);
    }
}, { once: true });