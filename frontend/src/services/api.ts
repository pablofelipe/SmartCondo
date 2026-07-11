import { getAuthHeaders } from '../utils/ApiUtils';
import config from '../config';

type RequestMethod = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';

interface ApiOptions extends RequestInit {
  expectNoContent: boolean;
  method?: RequestMethod;
  headers?: Record<string, string>;
  body?: any;
}

export const fetchApi = async <T>(
  endpoint: string,
  options: ApiOptions = {
    expectNoContent: false,
  },
): Promise<T> => {
  const authHeaders = getAuthHeaders();

  const headers: Record<string, string> = {
    ...authHeaders,
    ...options.headers,
  };

  const response = await fetch(`${config.apiUrl}${endpoint}`, {
    ...options,
    headers,
    body: options.body ? JSON.stringify(options.body) : undefined,
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({}));
    throw new Error(error.message || 'Erro na requisição');
  }

  // Se for void (como no markAsRead), não tenta parsear JSON
  if (response.status === 204 || options.expectNoContent) {
    return undefined as unknown as T;
  }

  return response.json() as Promise<T>;
};

// E então você pode usar:
export const markAsRead = async (messageId: number): Promise<void> => {
  return fetchApi<void>(`/Messages/${messageId}/read`, {
    method: 'PATCH',
    expectNoContent: true, // Nova flag opcional
  });
};
