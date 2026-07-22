// utils/notifications.js
export const requestNotificationPermission = async () => {
    if (!('Notification' in window)) {
        console.log('Notifications not supported');
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

    // Check whether a Service Worker is available for push notifications
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
        // Fallback to basic notifications
        new Notification(title, { body, icon });
    }
};