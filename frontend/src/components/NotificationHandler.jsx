// components/NotificationHandler.jsx
import { useWebSocket } from '../pages/hooks/useWebSocket';
import { showNotification } from '../utils/notifications';

const NotificationHandler = ({ userId }) => {
  const handleWebSocketMessage = async (data) => {
    if (data.type === 'NEW_MESSAGE') {
      // Mostrar notificação nativa
      await showNotification({
        title: 'Nova Mensagem',
        body: data.message.content,
        icon: '/icon-192.png',
      });

      // Ex: context/messagesContext.updateMessages(data.message);
    }
  };

  useWebSocket(userId, handleWebSocketMessage);

  return null; // Componente não renderiza nada visível
};

export default NotificationHandler;
