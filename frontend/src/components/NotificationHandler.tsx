import React, { useCallback } from 'react';
import {
  useWebSocket,
  WebSocketMessage,
} from '../pages/hooks/useWebSocket';
import { showNotification } from '../utils/notifications';

interface NotificationHandlerProps {
  userId: number | null;
}

interface NewMessageEvent extends WebSocketMessage {
  type: 'NEW_MESSAGE';
  message: {
    content: string;
  };
}

const isNewMessageEvent = (
  data: WebSocketMessage,
): data is NewMessageEvent => data.type === 'NEW_MESSAGE';

const NotificationHandler: React.FC<NotificationHandlerProps> = ({
  userId,
}) => {
  const handleWebSocketMessage = useCallback(async (data: WebSocketMessage) => {
    if (isNewMessageEvent(data)) {
      // Show the native notification
      await showNotification({
        title: 'New Message',
        body: data.message.content,
        icon: '/icon-192.png',
      });

      // E.g.: context/messagesContext.updateMessages(data.message);
    }
  }, []);

  useWebSocket(userId, handleWebSocketMessage);

  return null; // This component renders nothing visible
};

export default NotificationHandler;
