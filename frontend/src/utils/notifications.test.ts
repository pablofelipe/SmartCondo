import {
  requestNotificationPermission,
  showNotification,
} from './notifications';

describe('requestNotificationPermission', () => {
  const originalNotification = (global as any).Notification;

  afterEach(() => {
    (global as any).Notification = originalNotification;
  });

  it('returns false when the Notification API is not supported', async () => {
    delete (global as any).Notification;

    await expect(requestNotificationPermission()).resolves.toBe(false);
  });

  it('returns true without prompting when permission is already granted', async () => {
    const requestPermission = jest.fn();
    (global as any).Notification = { permission: 'granted', requestPermission };

    await expect(requestNotificationPermission()).resolves.toBe(true);
    expect(requestPermission).not.toHaveBeenCalled();
  });

  it('prompts for permission and resolves based on the user response', async () => {
    const requestPermission = jest.fn().mockResolvedValue('denied');
    (global as any).Notification = { permission: 'default', requestPermission };

    await expect(requestNotificationPermission()).resolves.toBe(false);
    expect(requestPermission).toHaveBeenCalled();
  });
});

describe('showNotification', () => {
  const originalNotification = (global as any).Notification;

  afterEach(() => {
    (global as any).Notification = originalNotification;
  });

  it('does nothing when notification permission is not granted', async () => {
    const requestPermission = jest.fn().mockResolvedValue('denied');
    (global as any).Notification = { permission: 'default', requestPermission };
    const notificationConstructor = jest.fn();
    (global as any).Notification = Object.assign(notificationConstructor, {
      permission: 'default',
      requestPermission,
    });

    await showNotification({ title: 'Title', body: 'Body' });

    expect(notificationConstructor).not.toHaveBeenCalled();
  });

  it('falls back to the basic Notification constructor when no service worker is available', async () => {
    const notificationConstructor = jest.fn();
    (global as any).Notification = Object.assign(notificationConstructor, {
      permission: 'granted',
      requestPermission: jest.fn(),
    });

    await showNotification({ title: 'Title', body: 'Body', icon: 'icon.png' });

    expect(notificationConstructor).toHaveBeenCalledWith('Title', {
      body: 'Body',
      icon: 'icon.png',
    });
  });
});
