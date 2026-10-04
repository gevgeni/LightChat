// js/ui/modals.js

import { state } from '../state.js';
import { chatsApi, usersApi } from '../api.js';
import { loadUserChats, selectChat } from './chats.js';
import { loadChatMembers } from './members.js';

// ==================== CREATE CHAT ====================

export function openCreateChatModal() {
    document.getElementById("createChatModal").style.display = "flex";
    const input = document.getElementById("newChatNameInput");
    input.value = "";
    input.focus();
}

export function closeCreateChatModal() {
    document.getElementById("createChatModal").style.display = "none";
}

export async function submitCreateChat() {
    const input = document.getElementById("newChatNameInput");
    const chatName = input.value.trim();

    if (!chatName) {
        alert("Название чата не может быть пустым");
        return;
    }

    try {
        const newChat = await chatsApi.create(chatName);
        closeCreateChatModal();
        await loadUserChats();
        selectChat(newChat.id, newChat.name);
    } catch (err) {
        console.error("[Modals] Ошибка создания чата:", err);
        alert(err.message);
    }
}

// ==================== DIRECT CHAT ====================

export function openDirectChatModal() {
    const modal = document.getElementById("directChatModal");
    const container = document.getElementById("directChatUsersList");
    container.innerHTML = "<div class='text-center text-slate-400 py-4 font-medium'>Загрузка пользователей...</div>";
    modal.style.display = "flex";

    usersApi.getAll()
        .then(users => {
            container.innerHTML = "";
            if (users.length === 0) {
                container.innerHTML = "<div class='text-center text-slate-400 py-4 font-medium'>Нет доступных пользователей</div>";
                return;
            }

            users.forEach(user => {
                const row = document.createElement("div");
                row.className = "p-3 bg-slate-50 border border-slate-100 rounded-xl cursor-pointer flex justify-between items-center hover:bg-slate-100 transition";

                const statusClass = user.isOnline ? "online" : "offline";
                row.innerHTML = `
                    <div class="flex items-center gap-2.5 min-w-0">
                        <div class="member-status w-2 h-2 rounded-full ${statusClass} shrink-0"></div>
                        <span class="font-semibold text-sm text-slate-700 truncate">${user.username}</span>
                    </div>
                    <button class="bg-indigo-600 hover:bg-indigo-700 text-white font-bold py-1.5 px-3 rounded-lg text-xs transition shrink-0 shadow shadow-indigo-600/10">Написать</button>
                `;

                row.onclick = () => submitStartDirectChat(user.id, user.username);
                container.appendChild(row);
            });
        })
        .catch(err => {
            console.error("[Modals] Ошибка загрузки пользователей:", err);
            container.innerHTML = "<div class='text-center text-rose-500 py-4 font-medium'>Ошибка загрузки</div>";
        });
}

export function closeDirectChatModal() {
    document.getElementById("directChatModal").style.display = "none";
}

export async function submitStartDirectChat(targetUserId, targetUsername) {
    try {
        const chat = await chatsApi.createDirect(targetUserId);
        closeDirectChatModal();
        await loadUserChats();
        selectChat(chat.id, targetUsername);
    } catch (err) {
        console.error("[Modals] Ошибка создания личного чата:", err);
        alert(err.message);
    }
}

// ==================== ADD MEMBER ====================

export async function openAddMemberModal() {
    if (!state.activeChatId) return;

    const modal = document.getElementById("addMemberModal");
    const container = document.getElementById("addMemberUsersList");
    container.innerHTML = "<div class='text-center text-slate-400 py-4 font-medium'>Загрузка...</div>";
    modal.style.display = "flex";

    try {
        const users = await usersApi.getAll();
        container.innerHTML = "";

        if (users.length === 0) {
            container.innerHTML = "<div class='text-center text-slate-400 py-4 font-medium'>Нет доступных пользователей</div>";
            return;
        }

        users.forEach(user => {
            const row = document.createElement("div");
            row.className = "p-3 bg-slate-50 border border-slate-100 rounded-xl cursor-pointer flex justify-between items-center hover:bg-slate-100 transition";

            const statusClass = user.isOnline ? "online" : "offline";
            row.innerHTML = `
                <div class="flex items-center gap-2.5 min-w-0">
                    <div class="member-status w-2 h-2 rounded-full ${statusClass} shrink-0"></div>
                    <span class="font-semibold text-sm text-slate-700 truncate">${user.username}</span>
                </div>
                <button class="bg-emerald-500 hover:bg-emerald-600 text-white font-bold py-1.5 px-3 rounded-lg text-xs transition shrink-0 shadow shadow-emerald-500/10">Пригласить</button>
            `;

            row.onclick = () => submitAddMember(user.id);
            container.appendChild(row);
        });
    } catch (err) {
        console.error("[Modals] Ошибка загрузки пользователей:", err);
        container.innerHTML = "<div class='text-center text-rose-500 py-4 font-medium'>Ошибка загрузки</div>";
    }
}

export function closeAddMemberModal() {
    document.getElementById("addMemberModal").style.display = "none";
}

export async function submitAddMember(targetUserId) {
    try {
        await chatsApi.addMember(state.activeChatId, targetUserId);
        closeAddMemberModal();
        await loadChatMembers(state.activeChatId);
    } catch (err) {
        console.error("[Modals] Ошибка добавления участника:", err);
        alert(err.message);
    }
}

// ==================== LEAVE / DELETE CHAT ====================

export async function leaveChat() {
    if (!state.activeChatId) return;
    if (!confirm("Вы уверены, что хотите покинуть этот чат?")) return;

    try {
        await chatsApi.leave(state.activeChatId);
        alert("Вы покинули чат.");
        await loadUserChats();
        document.getElementById("activeChatArea").style.display = "none";
        document.getElementById("noChatSelected").style.display = "flex";
        state.activeChatId = null;
    } catch (err) {
        alert("Ошибка: " + err.message);
    }
}

export async function deleteChat() {
    if (!state.activeChatId) return;
    if (!confirm("Удаление чата удалит все сообщения для всех участников. Продолжить?")) return;

    try {
        await chatsApi.delete(state.activeChatId);
        alert("Чат удалён.");
        await loadUserChats();
        document.getElementById("activeChatArea").style.display = "none";
        document.getElementById("noChatSelected").style.display = "flex";
        state.activeChatId = null;
    } catch (err) {
        alert("Ошибка: " + err.message);
    }
}