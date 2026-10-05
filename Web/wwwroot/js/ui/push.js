// js/ui/push.js

import { state } from '../state.js';
import { loadUserChats, selectChat } from './chats.js';
import { toggleMobileSidebar } from './mobile.js';

// ==================== КНОПКА ====================

export async function handleEnablePush() {
    const btn = document.getElementById("pushNotifyBtn");
    if (!btn) return;

    btn.disabled = true;
    btn.innerText = "⏳ Подписка...";

    const ok = await window.enablePushNotifications(state.jwtToken);

    btn.disabled = false;
    updatePushButton(ok);

    if (!ok && Notification.permission === 'denied') {
        alert("Вы отклонили уведомления. Разрешите их в настройках браузера для этого сайта.");
    }
}

export function updatePushButton(subscribed) {
    const btn = document.getElementById("pushNotifyBtn");
    if (!btn) return;

    if (Notification.permission === 'denied') {
        btn.innerText = "🔕 Уведомления запрещены";
        btn.classList.add("opacity-60");
        btn.disabled = true;
        return;
    }

    if (subscribed) {
        btn.innerText = "✅ Уведомления включены";
        btn.classList.remove("bg-slate-700", "hover:bg-slate-600");
        btn.classList.add("bg-emerald-600", "hover:bg-emerald-500");
    } else {
        btn.innerText = "🔔 Включить уведомления";
        btn.classList.remove("bg-emerald-600", "hover:bg-emerald-500");
        btn.classList.add("bg-slate-700", "hover:bg-slate-600");
    }
}

export async function initializePushNotifications() {
    if (!('serviceWorker' in navigator) || !('PushManager' in window)) {
        console.warn("[Push] Не поддерживается этим браузером");
        return;
    }

    try {
        await navigator.serviceWorker.register('/service-worker.js');
        await navigator.serviceWorker.ready;
        console.log("[Push] Service Worker зарегистрирован");
    } catch (err) {
        console.error("[Push] Ошибка регистрации SW:", err);
        return;
    }

    if (Notification.permission === 'granted') {
        const ok = await window.enablePushNotifications(state.jwtToken);
        updatePushButton(ok);
    } else {
        updatePushButton(false);
    }
}

// ==================== DEEP LINK (открытие чата из push) ====================

export function consumeOpenChatFromUrl() {
    const params = new URLSearchParams(window.location.search);
    const chatId = params.get('openChat');
    if (!chatId) {
        console.log('[Push] ?openChat не найден в URL');
        return;
    }

    state.pendingChatToOpen = chatId;
    console.log('[Push] Отложен чат для открытия:', chatId);

    // Очищаем URL
    const url = new URL(window.location.href);
    url.searchParams.delete('openChat');
    const clean = url.pathname + (url.search ? url.search : '') + url.hash;
    window.history.replaceState({}, '', clean);
}

export function setupServiceWorkerListener() {
    if (!('serviceWorker' in navigator)) return;

    navigator.serviceWorker.addEventListener('message', (event) => {
        if (event.data?.type === 'openChat' && event.data.chatId) {
            console.log('[Push] SW передал чат:', event.data.chatId);
            state.pendingChatToOpen = event.data.chatId;
            tryOpenPendingChat();
        }
    });
}

export async function tryOpenPendingChat() {
    console.log('[Push] tryOpenPendingChat, pending =', state.pendingChatToOpen);

    if (!state.pendingChatToOpen) return false;

    if (!state.jwtToken || !state.currentUserId) {
        console.log('[Push] Ожидание авторизации для открытия чата:', state.pendingChatToOpen);
        return false;
    }

    const chatId = state.pendingChatToOpen;
    let item = document.getElementById(`chat-item-${chatId}`);

    if (!item) {
        console.log('[Push] Чат не найден в DOM, обновляем список чатов');
        await loadUserChats();
        item = document.getElementById(`chat-item-${chatId}`);
    }

    if (!item) {
        console.warn('[Push] Чат недоступен:', chatId);
        state.pendingChatToOpen = null;
        return false;
    }

    const name = item.querySelector('.chat-item-name')?.innerText || 'Чат';
    console.log('[Push] Открываем чат:', chatId, name);
    await selectChat(chatId, name);
    toggleMobileSidebar(false);
    state.pendingChatToOpen = null;
    console.log('[Push] Чат открыт:', chatId);
    return true;
}