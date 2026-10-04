// js/ui/chats.js

import { state } from '../state.js';
import { chatsApi } from '../api.js';
import { hub } from '../signalr.js';
import { toggleMobileSidebar } from './mobile.js';
import { loadMoreMessages, handleReceiveMessage } from './messages.js';
import { loadChatMembers, removeMemberFromList } from './members.js';

/**
 * Загрузка списка чатов пользователя
 */
export async function loadUserChats() {
    try {
        const chats = await chatsApi.getMyChats();

        const container = document.getElementById("chatsListContainer");
        container.innerHTML = "";

        chats.forEach(chat => {
            const item = document.createElement("div");
            item.className = "chat-item px-4 py-3.5 flex justify-between items-center cursor-pointer hover:bg-slate-700/60 transition duration-150 text-sm font-medium border-b border-slate-700/30";
            item.id = `chat-item-${chat.id}`;
            item.dataset.isDirect = chat.isDirect;
            item.onclick = () => selectChat(chat.id, chat.name);

            const unreadCount = chat.unreadCount || 0;
            const badgeDisplay = unreadCount > 0 ? 'inline-block' : 'none';

            item.innerHTML = `
                <span class="chat-item-name truncate flex-1 pr-2">${chat.name}</span>
                <span class="unread-badge bg-emerald-500 text-white rounded-full px-2 py-0.5 text-xs font-bold min-w-[20px] text-center shrink-0 shadow-sm shadow-emerald-500/20" style="display: ${badgeDisplay};">${unreadCount}</span>
            `;

            container.appendChild(item);

            if (state.connection && state.connection.state === signalR.HubConnectionState.Connected) {
                hub.joinChat(chat.id).catch(err =>
                    console.error(`[Chats] Ошибка фонового подключения к чату ${chat.id}:`, err)
                );
            }
        });
    } catch (err) {
        console.error("[Chats] Ошибка загрузки списка чатов:", err);
    }
}

/**
 * Выбор чата (переключение)
 */
export async function selectChat(chatId, chatName) {
    if (state.activeChatId === chatId) return;

    if (state.activeChatId) {
        document.getElementById(`chat-item-${state.activeChatId}`)?.classList.remove("active");
    }
    state.activeChatId = chatId;

    const currentChatItem = document.getElementById(`chat-item-${state.activeChatId}`);
    const isDirect = currentChatItem ? currentChatItem.dataset.isDirect === 'true' : false;
    state.isActiveChatDirect = isDirect;

    const addMemberBtn = document.querySelector(".members-header button");
    if (addMemberBtn) addMemberBtn.style.display = isDirect ? "none" : "flex";

    if (currentChatItem) {
        currentChatItem.classList.add("active");
        currentChatItem.style.fontWeight = "normal";
        const badge = currentChatItem.querySelector('.unread-badge');
        if (badge) {
            badge.innerText = "0";
            badge.style.display = "none";
        }
    }

    state.oldestMessageId = null;
    state.hasMoreMessages = true;
    document.getElementById("messagesList").innerHTML = "";

    document.getElementById("noChatSelected").style.display = "none";
    document.getElementById("activeChatArea").classList.remove("hidden");
    document.getElementById("activeChatArea").style.display = "flex";
    document.getElementById("currentChatTitle").innerText = chatName;

    await loadMoreMessages();
    await loadChatMembers(chatId);

    try {
        await hub.joinChat(chatId);
    } catch (err) {
        console.error("[Chats] SignalR JoinChat error:", err);
    }

    // Закрываем мобильный sidebar после выбора чата
    toggleMobileSidebar(false);
}

// ==================== ОБРАБОТЧИКИ SIGNALR ====================

export function handleChatInvitation() {
    console.log("[Chats] Получено приглашение в новый чат. Обновляем список...");
    loadUserChats();
}

export function handleChatDeleted(data) {
    const chatId = data.chatId;
    const item = document.getElementById(`chat-item-${chatId}`);
    if (!item) return;

    item.remove();

    if (state.activeChatId === chatId) {
        document.getElementById("activeChatArea").style.display = "none";
        document.getElementById("noChatSelected").style.display = "flex";
        state.activeChatId = null;
    }
}

export function handleUserLeave(data) {
    // Если вышел другой пользователь в активном чате — убираем его из списка участников
    if (data.chatId === state.activeChatId && data.userId !== state.currentUserId) {
        removeMemberFromList(data.userId);
    }
    // Если я вышел сам — чат удаляется из списка
    if (data.userId === state.currentUserId) {
        const item = document.getElementById(`chat-item-${data.chatId}`);
        if (item) item.remove();

        if (state.activeChatId === data.chatId) {
            document.getElementById("activeChatArea").style.display = "none";
            document.getElementById("noChatSelected").style.display = "flex";
            state.activeChatId = null;
        }
    }
}