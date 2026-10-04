// js/ui/messages.js

import { state } from '../state.js';
import { formatTime, scrollToBottom as utilsScrollToBottom, playSystemNotificationSound } from '../utils.js';
import { messagesApi } from '../api.js';
import { hub } from '../signalr.js';
import { MESSAGES_PAGE_SIZE, TYPING_NOTIFY_INTERVAL } from '../config.js';

/**
 * Загрузка порции истории сообщений (с пагинацией)
 */
export async function loadMoreMessages() {
    if (state.isLoadingMessages || !state.hasMoreMessages) return;
    if (!state.activeChatId) return;

    state.isLoadingMessages = true;

    const list = document.getElementById("messagesList");
    const previousScrollHeight = list.scrollHeight;

    try {
        const messages = await messagesApi.getHistory(
            state.activeChatId,
            MESSAGES_PAGE_SIZE,
            state.oldestMessageId
        );

        if (messages.length === 0) {
            state.hasMoreMessages = false;
            return;
        }

        state.oldestMessageId = messages[0].id;

        for (let i = messages.length - 1; i >= 0; i--) {
            appendSingleMessage(messages[i], true);
        }

        if (state.connection && state.connection.state === signalR.HubConnectionState.Connected) {
            hub.markChatAsRead(state.activeChatId).catch(err => console.error(err));
        }

        if (previousScrollHeight > 0) {
            list.scrollTop = list.scrollHeight - previousScrollHeight;
        } else {
            list.scrollTop = list.scrollHeight;
        }
    } catch (err) {
        console.error('[Messages] Ошибка загрузки истории:', err);
    } finally {
        state.isLoadingMessages = false;
    }
}

/**
 * Триггер подгрузки при скролле вверх
 */
export function handleScroll() {
    const list = document.getElementById("messagesList");
    if (list && list.scrollTop === 0) {
        loadMoreMessages();
    }
}

/**
 * Добавление одного сообщения в DOM
 */
export function appendSingleMessage(message, prepend = false) {
    const list = document.getElementById("messagesList");
    const msgDiv = document.createElement("div");
    msgDiv.classList.add("msg");

    const username = message.senderUsername || (message.sender ? message.sender.username : null);
    const nameToShow = username || `Пользователь [${message.senderId.substring(0, 5)}]`;
    const nameSpan = state.isActiveChatDirect ? '' : `<span class="msg-sender">${nameToShow}</span>`;

    const isMyMessage = String(message.senderId).toLowerCase() === String(state.currentUserId).toLowerCase();

    if (isMyMessage) {
        const isRead = message.isRead || message.IsRead;
        const checkmarks = isRead ? '✓✓' : '✓';

        const statusHtml = `<span class="msg-status" style="margin-right: 5px; color: #c7d2fe; font-weight: bold;">${checkmarks}</span>`;

        msgDiv.classList.add("outgoing");
        msgDiv.innerHTML = `${message.text}<span class="msg-sentAt">${statusHtml}${formatTime(message.sentAt)}</span>`;
    } else {
        msgDiv.classList.add("incoming");
        msgDiv.innerHTML = `${nameSpan}${message.text}<span class="msg-sentAt">${formatTime(message.sentAt)}</span>`;
    }

    if (prepend) {
        list.insertBefore(msgDiv, list.firstChild);
    } else {
        list.appendChild(msgDiv);
    }
}

/**
 * Отправка сообщения в активный чат
 */
export async function sendMessage() {
    const textInput = document.getElementById("messageTextInput");
    const text = textInput.value.trim();
    if (!text || !state.connection || !state.activeChatId) return;

    try {
        await hub.sendMessage(state.activeChatId, text);
        textInput.value = "";
        requestAnimationFrame(() => scrollToBottom(true));
    } catch (err) {
        console.error("[Messages] Ошибка отправки:", err);
        alert("Ошибка при отправке: " + err.message);
    }
}

/**
 * Прокрутить вниз, если пользователь уже внизу
 */
export function scrollToBottom(force = false) {
    const container = document.getElementById('messagesList');
    utilsScrollToBottom(container, force);
}

/**
 * Обработчики входящих сообщений
 */
export function handleReceiveMessage(message) {
    if (message.chatId === state.activeChatId) {
        const list = document.getElementById("messagesList");
        const wasAtBottom = list.scrollHeight - list.scrollTop - list.clientHeight <= 50;

        appendSingleMessage(message, false);

        if (wasAtBottom) {
            // Если пользователь был внизу — скроллим вниз принудительно
            requestAnimationFrame(() => {
                list.scrollTop = list.scrollHeight;
            });
        }

        requestAnimationFrame(() => scrollToBottom(false));

        const isNotMyMessage = String(message.senderId).toLowerCase() !== String(state.currentUserId).toLowerCase();
        if (isNotMyMessage) {
            hub.markChatAsRead(message.chatId).catch(err =>
                console.error("[Messages] Ошибка авто-прочтения:", err));
        }
    } else {
        console.log(`[Messages] Сообщение в фоновый чат: ${message.chatId}`);

        // 1. Звук уведомления
        playSystemNotificationSound();

        // 2. Обновляем UI списка чатов: badge + bold
        const chatItem = document.getElementById(`chat-item-${message.chatId}`);
        if (chatItem) {
            chatItem.style.fontWeight = "bold";

            const badge = chatItem.querySelector('.unread-badge');
            if (badge) {
                let currentCount = parseInt(badge.innerText) || 0;
                currentCount++;
                badge.innerText = currentCount;
                badge.style.display = 'inline-block';
            }
        }
    }
}

export function handleMessagesMarkedAsRead(data) {
    const isCurrentChat = String(data.chatId) === String(state.activeChatId);
    const isReaderMe = String(data.readerId).toLowerCase() === String(state.currentUserId).toLowerCase();

    if (isCurrentChat && !isReaderMe) {
        const statuses = document.querySelectorAll('#messagesList .outgoing .msg-status');
        statuses.forEach(el => {
            if (el.innerText.trim() === '✓') el.innerText = '✓✓';
        });
    }

    if (isReaderMe) {
        const chatItem = document.getElementById(`chat-item-${data.chatId}`);
        if (chatItem) {
            chatItem.style.fontWeight = "normal";
            const badge = chatItem.querySelector('.unread-badge');
            if (badge) { badge.innerText = "0"; badge.style.display = "none"; }
        }
    }
}

/**
 * Обработчик события "пользователь печатает"
 */
let hideTypingIndicatorTimer;

export function handleUserIsTyping(data) {
    if (data.chatId !== state.activeChatId) return;

    const indicator = document.getElementById("typingIndicator");
    if (!indicator) return;

    const memberElement = document.querySelector(`#chatMembersContainer [data-member-id="${data.userId}"] span`);
    const username = memberElement ? memberElement.innerHTML.replace(" (Вы)", "") : "Собеседник";

    indicator.innerText = `${username} печатает...`;
    indicator.style.display = "block";

    clearTimeout(hideTypingIndicatorTimer);
    hideTypingIndicatorTimer = setTimeout(() => {
        indicator.style.display = "none";
    }, 2000);
}

/**
 * Инициализация: обработчик скролла + отправка NotifyTyping при вводе
 */
let lastTypingTime = 0;

export function initializeMessagesHandlers() {
    const list = document.getElementById("messagesList");
    if (list) {
        list.addEventListener("scroll", handleScroll);
    }

    const input = document.getElementById("messageTextInput");
    if (input) {
        input.addEventListener("input", () => {
            if (!state.activeChatId || !state.connection) return;
            if (state.connection.state !== signalR.HubConnectionState.Connected) return;

            const now = Date.now();
            if (now - lastTypingTime > TYPING_NOTIFY_INTERVAL) {
                hub.notifyTyping(state.activeChatId).catch(err =>
                    console.error("[Messages] Ошибка NotifyTyping:", err));
                lastTypingTime = now;
            }
        });
    }
}