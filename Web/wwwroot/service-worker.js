// service-worker.js

self.addEventListener('install', (event) => {
    self.skipWaiting();
});

self.addEventListener('activate', (event) => {
    event.waitUntil(self.clients.claim());
});

self.addEventListener('push', (event) => {
    let data = {};
    try {
        data = event.data ? event.data.json() : {};
    } catch (e) {
        data = { title: 'LightChat', body: event.data ? event.data.text() : '' };
    }

    const title = data.title || 'Новое сообщение';
    const options = {
        body: data.body || '',
        icon: '/web-app-manifest-192x192.png',
        badge: '/favicon-96x96.png',
        tag: data.chatId || 'lightchat',
        renotify: true,
        data: { chatId: data.chatId }
    };

    event.waitUntil(self.registration.showNotification(title, options));
});

self.addEventListener('notificationclick', (event) => {
    event.notification.close();
    const chatId = event.notification.data && event.notification.data.chatId;

    event.waitUntil(
        self.clients.matchAll({ type: 'window', includeUncontrolled: true }).then((clientList) => {
            // 1. Приоритет — уже сфокусированная вкладка
            let target = clientList.find(c => c.focused);

            // 2. Иначе — любая открытая
            if (!target && clientList.length > 0) target = clientList[0];

            if (target) {
                target.postMessage({ type: 'openChat', chatId });
                return target.focus();
            }

            // 3. Иначе — новое окно с параметром в URL
            const url = chatId ? '/?openChat=' + encodeURIComponent(chatId) : '/';
            return self.clients.openWindow(url);
        })
    );
});