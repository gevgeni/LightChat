// js/ui/push.js

import { state } from '../state.js';

/**
 * Клик по кнопке "🔔 Включить уведомления"
 */
export async function handleEnablePush() {
    const btn = document.getElementById("pushNotifyBtn");
    if (!btn) return;

    btn.disabled = true;
    btn.innerText = "⏳ Подписка...";

    // push-notifications.js подключён как обычный <script>, функция глобальная
    const ok = await window.enablePushNotifications(state.jwtToken);

    btn.disabled = false;
    updatePushButton(ok);

    if (!ok && Notification.permission === 'denied') {
        alert("Вы отклонили уведомления. Разрешите их в настройках браузера для этого сайта.");
    }
}

/**
 * Обновить внешний вид кнопки в зависимости от состояния подписки
 */
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

/**
 * Инициализация push после логина:
 * 1. Регистрируем Service Worker
 * 2. Если разрешение уже дано — подписываемся
 * 3. Обновляем кнопку
 */
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