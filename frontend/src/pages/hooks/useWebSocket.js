// hooks/useWebSocket.js
import config from '../../config';
import { useEffect, useRef, useCallback } from 'react';

export const useWebSocket = (userId, onMessage) => {
  const ws = useRef(null);

  const connect = useCallback(() => {
    const token = localStorage.getItem('token');
    const websocketUrl = `${config.apiGatewayUrl}?token=${encodeURIComponent(token)}`;

    ws.current = new WebSocket(websocketUrl);

    ws.current.onopen = () => {
      console.log('WebSocket conectado');
      // Reconectar automaticamente se desconectado
      localStorage.setItem('websocket_connected', 'true');
    };

    ws.current.onmessage = (event) => {
      try {
        const data = JSON.parse(event.data);
        onMessage(data);
      } catch (error) {
        console.error('Erro ao processar mensagem:', error);
      }
    };

    ws.current.onclose = () => {
      console.log('WebSocket desconectado');
      localStorage.removeItem('websocket_connected');

      // Tentar reconectar após 5 segundos
      setTimeout(() => connect(), 5000);
    };

    ws.current.onerror = (error) => {
      console.error('Erro WebSocket:', error);
    };
  }, [userId, onMessage]);

  useEffect(() => {
    if (userId) {
      connect();
    }

    return () => {
      if (ws.current) {
        ws.current.close();
      }
    };
  }, [userId, connect]);

  return { ws };
};
