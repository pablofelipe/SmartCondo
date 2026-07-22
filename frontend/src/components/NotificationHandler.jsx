// components/NotificationHandler.jsx
import { useWebSocket } from '../pages/hooks/useWebSocket';
import { showNotification } from '../utils/notifications';

const NotificationHandler = ({ userId }) => {
  const handleWebSocketMessage = async (data) => {
    if (data.type === 'NEW_MESSAGE') {
      // Show the native notification
      await showNotification({
        title: 'New Message',
        body: data.message.content,
        icon: '/icon-192.png',
      });

      // E.g.: context/messagesContext.updateMessages(data.message);
    }
  };

  useWebSocket(userId, handleWebSocketMessage);

  return null; // This component renders nothing visible
};

export default NotificationHandler;
