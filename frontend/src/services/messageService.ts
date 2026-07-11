import { fetchApi } from './api';
import {
  convertScopeToBackend,
  Message,
  MessageCreateDto,
} from '../types/message';

export const getReceivedMessages = async (): Promise<Message[]> => {
  return fetchApi<Message[]>('/Messages/received');
};

export const getSentMessages = async (): Promise<Message[]> => {
  return fetchApi<Message[]>('/Messages/sent');
};

export const sendMessage = async (
  messageData: MessageCreateDto,
): Promise<Message> => {
  const payload = {
    ...messageData,
    scope: convertScopeToBackend(messageData.scope),
  };

  return fetchApi<Message>('/Messages', {
    method: 'POST',
    body: payload,
    expectNoContent: false,
  });
};

export const markAsRead = async (messageId: number): Promise<void> => {
  return fetchApi<void>(`/Messages/${messageId}/read`, {
    method: 'PATCH',
    expectNoContent: true,
  });
};
