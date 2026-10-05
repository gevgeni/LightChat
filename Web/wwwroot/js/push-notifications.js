// push-notifications.js

function urlBase64ToUint8Array(base64String) {
    const padding = '='.repeat((4 - (base64String.length % 4)) % 4);
    const base64 = (base64String + padding).replace(/-/g, '+').replace(/_/g, '/');
    const rawData = window.atob(base64);
    const outputArray = new Uint8Array(rawData.length);
    for (let i = 0; i < rawData.length; ++i) {
        outputArray[i] = rawData.charCodeAt(i);
    }
    return outputArray;
}

async function enablePushNotifications(jwtToken) {
    if (!('serviceWorker' in navigator) || !('PushManager' in window)) {
        console.warn('[Push] Не поддерживается этим браузером.');
        return false;
    }

    try {
        // 1. Регистрируем service worker
        const registration = await navigator.serviceWorker.register('/service-worker.js');
        await navigator.serviceWorker.ready;

        // 2. Запрашиваем разрешение
        let permission = Notification.permission;
        if (permission === 'default') {
            permission = await Notification.requestPermission();
        }
        if (permission !== 'granted') {
            console.warn('[Push] Разрешение не получено:', permission);
            return false;
        }

        // 3. Получаем публичный VAPID-ключ
        const vapidResp = await fetch('/notifications/vapid-public-key');
        if (!vapidResp.ok) throw new Error('Не удалось получить VAPID ключ');
        const { publicKey } = await vapidResp.json();

        // 4. Подписываемся (или берём уже существующую подписку)
        let subscription = await registration.pushManager.getSubscription();
        if (!subscription) {
            subscription = await registration.pushManager.subscribe({
                userVisibleOnly: true,
                applicationServerKey: urlBase64ToUint8Array(publicKey)
            });
        }

        // 5. Отправляем подписку на сервер
        const subJson = subscription.toJSON();
        const saveResp = await fetch('/notifications/subscribe', {
            method: 'POST',
            headers: {
                'Authorization': `Bearer ${jwtToken}`,
                'Content-Type': 'application/json'
            },
            body: JSON.stringify({
                endpoint: subJson.endpoint,
                p256dh: subJson.keys.p256dh,
                auth: subJson.keys.auth
            })
        });

        if (!saveResp.ok) throw new Error('Сервер отклонил подписку');

        console.log('[Push] Подписка успешно оформлена.');
        return true;
    } catch (err) {
        console.error('[Push] Ошибка подписки:', err);
        return false;
    }
}