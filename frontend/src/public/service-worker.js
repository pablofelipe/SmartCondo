this.addEventListener('install', (event) => {
    this.skipWaiting();
    console.log('Service Worker instalado');
});

this.addEventListener('activate', (event) => {
    event.waitUntil(this.clients.claim());
    console.log('Service Worker ativado');
});

this.addEventListener('push', (event) => {
    if (!event.data) return;

    const data = event.data.json();

    const options = {
        body: data.body,
        icon: data.icon || '/icon-192.png',
        badge: '/icon-72.png',
        vibrate: [200, 100, 200],
        data: data.data
    };

    event.waitUntil(
        this.registration.showNotification(data.title, options)
    );
});

this.addEventListener('notificationclick', (event) => {
    event.notification.close();

    event.waitUntil(
        this.clients.matchAll({ type: 'window' }).then((clientList) => {
            if (clientList.length > 0) {
                return clientList[0].focus();
            }
            return this.clients.openWindow('/messages');
        })
    );
});