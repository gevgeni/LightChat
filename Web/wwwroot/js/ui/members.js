// js/ui/members.js

import { state } from '../state.js';
import { chatsApi } from '../api.js';

/**
 * Загрузка и рендер списка участников чата
 */
export async function loadChatMembers(chatId) {
    try {
        const members = await chatsApi.getMembers(chatId);

        const container = document.getElementById("chatMembersContainer");
        const countSpan = document.getElementById("membersCount");

        container.innerHTML = "";
        countSpan.innerText = members.length;

        members.forEach(member => {
            const item = document.createElement("div");
            item.className = "member-item flex items-center gap-3 p-2.5 rounded-xl hover:bg-slate-50 transition cursor-pointer text-sm font-medium text-slate-700";
            item.setAttribute("data-member-id", member.id);

            const isMe = member.id === state.currentUserId ? " (Вы)" : "";
            const statusClass = (member.id === state.currentUserId || member.isOnline) ? "online" : "offline";

            item.innerHTML = `<div class="member-status w-2.5 h-2.5 rounded-full ${statusClass}"></div><span class="truncate">${member.username}${isMe}</span>`;
            container.appendChild(item);
        });
    } catch (err) {
        console.error("[Members] Ошибка загрузки участников:", err);
    }
}

/**
 * Обновление индикатора онлайн/офлайн при SignalR-событии UserStatusChanged
 */
export function handleUserStatusChanged(data) {
    const memberRow = document.querySelector(`#chatMembersContainer [data-member-id="${data.userId}"]`);
    if (!memberRow) return;

    const statusIndicator = memberRow.querySelector('.member-status');
    if (!statusIndicator) return;

    if (data.isOnline) {
        statusIndicator.classList.remove('offline');
        statusIndicator.classList.add('online');
    } else {
        statusIndicator.classList.remove('online');
        statusIndicator.classList.add('offline');
    }
}

/**
 * Удаление участника из списка (при выходе из чата — событие UserLeave)
 */
export function removeMemberFromList(userId) {
    const memberRow = document.querySelector(`#chatMembersContainer [data-member-id="${userId}"]`);
    if (memberRow) {
        memberRow.remove();

        const countSpan = document.getElementById("membersCount");
        if (countSpan) {
            const current = parseInt(countSpan.innerText) || 0;
            countSpan.innerText = Math.max(0, current - 1);
        }
    }
}