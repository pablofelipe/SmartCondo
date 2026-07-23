import { useCallback, useEffect, useRef } from 'react';
import config from '../../config';

export interface WebSocketMessage {
  type: string;
  [key: string]: unknown;
}

export const useWebSocket = (
  userId: number | null,
  onMessage: (data: WebSocketMessage) => void,
) => {
  const ws = useRef<WebSocket | null>(null);
  const onMessageRef = useRef(onMessage);

  useEffect(() => {
    onMessageRef.current = onMessage;
  }, [onMessage]);

  const connect = useCallback(() => {
    const token = localStorage.getItem('token');
    const websocketUrl = `${config.websocketUrl}?token=${encodeURIComponent(token ?? '')}`;

    ws.current = new WebSocket(websocketUrl);

    ws.current.onopen = () => {
      console.log('WebSocket connected');
      // Automatically reconnect if disconnected
      localStorage.setItem('websocket_connected', 'true');
    };

    ws.current.onmessage = (event: MessageEvent) => {
      try {
        const data = JSON.parse(event.data);
        onMessageRef.current(data);
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

    ws.current.onerror = (error: Event) => {
      console.error('WebSocket error:', error);
    };
  }, []);

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
