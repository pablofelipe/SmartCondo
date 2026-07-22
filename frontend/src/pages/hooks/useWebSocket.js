// hooks/useWebSocket.js
import config from '../../config';
import { useEffect, useRef, useCallback } from 'react';

export const useWebSocket = (userId, onMessage) => {
  const ws = useRef(null);

  const connect = useCallback(() => {
    const token = localStorage.getItem('token');
    const websocketUrl = `${config.websocketUrl}?token=${encodeURIComponent(token)}`;

    ws.current = new WebSocket(websocketUrl);

    ws.current.onopen = () => {
      console.log('WebSocket connected');
      // Automatically reconnect if disconnected
      localStorage.setItem('websocket_connected', 'true');
    };

    ws.current.onmessage = (event) => {
      try {
        const data = JSON.parse(event.data);
        onMessage(data);
      } catch (error) {
        console.error('Error processing message:', error);
      }
    };

    ws.current.onclose = () => {
      console.log('WebSocket disconnected');
      localStorage.removeItem('websocket_connected');

      // Try to reconnect after 5 seconds
      setTimeout(() => connect(), 5000);
    };

    ws.current.onerror = (error) => {
      console.error('WebSocket error:', error);
    };
  }, [onMessage]);

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
