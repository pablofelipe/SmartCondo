import React from 'react';
import { Message } from '../../types/message';

interface MessageItemProps {
  message: Message;
  onMarkAsRead: (id: number) => void;
}

const MessageItem: React.FC<MessageItemProps> = ({ message, onMarkAsRead }) => {
  return (
    <div className={`message ${message.isRead ? 'read' : 'unread'}`}>
      <h3>
        {message.senderName} ({message.senderType})
      </h3>
      <p>{message.content}</p>
      <small>{new Date(message.sentDate).toLocaleString()}</small>
      {!message.isRead && (
        <button onClick={() => onMarkAsRead(message.id)}>
          Marcar como lida
        </button>
      )}
    </div>
  );
};

export default MessageItem;
