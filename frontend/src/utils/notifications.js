// utils/notifications.js
export const requestNotificationPermission = async () => {
    if (!('Notification' in window)) {
        console.log('Notificações não suportadas');
        return false;
    }

    if (Notification.permission === 'granted') {
        return true;
    }

    const permission = await Notification.requestPermission();
    return permission === 'granted';
};

export const showNotification = async ({ title, body, icon }) => {
    if (!await requestNotificationPermission()) {
        return;
    }

    // Verificar se Service Worker está disponível para notificações push
    if ('serviceWorker' in navigator && Notification.permission === 'granted') {
        const registration = await navigator.serviceWorker.ready;

        registration.showNotification(title, {
            body,
            icon: icon || '/icon-192.png',
            badge: '/icon-72.png',
            vibrate: [200, 100, 200],
            tag: 'new-message'
        });
    } else {
        // Fallback para notificações básicas
        new Notification(title, { body, icon });
    }
};