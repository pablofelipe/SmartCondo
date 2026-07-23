import { getAuthHeaders, getPublicHeaders } from '../utils/ApiUtils';
import config from '../config';

type RequestMethod = 'GET' | 'POST' | 'PUT' | 'PATCH' | 'DELETE';

interface ApiOptions extends Omit<RequestInit, 'body' | 'headers'> {
  expectNoContent?: boolean;
  method?: RequestMethod;
  headers?: Record<string, string>;
  body?: unknown;
  // Set to false for endpoints that don't require a session (login, forgot/reset
  // password). Defaults to true, since almost every endpoint needs the bearer token.
  authRequired?: boolean;
}

export const fetchApi = async <T>(
  endpoint: string,
  options: ApiOptions = {},
): Promise<T> => {
  const {
    expectNoContent = false,
    authRequired = true,
    headers: overrideHeaders,
    body,
    ...requestInit
  } = options;

  const baseHeaders = authRequired ? getAuthHeaders() : getPublicHeaders();

  const headers: Record<string, string> = {
    ...baseHeaders,
    ...overrideHeaders,
  };

  const response = await fetch(`${config.apiUrl}${endpoint}`, {
    ...requestInit,
    headers,
    body: body ? JSON.stringify(body) : undefined,
  });

  if (!response.ok) {
    const error = await response.json().catch(() => ({}));
    // ASP.NET Core's built-in [ApiController] model validation returns a
    // ValidationProblemDetails body ({ errors: { field: [msg, ...] } }) instead of
    // the app's own { message } contract - fold both shapes into one thrown message
    // here so no caller needs to know which one a given endpoint can return.
    const message = error.errors
      ? Object.values<string[]>(error.errors).flat().join('\n')
      : error.message || 'Request failed';
    throw new Error(message);
  }

  // For void responses (like markAsRead), don't try to parse JSON
  if (response.status === 204 || expectNoContent) {
    return undefined as unknown as T;
  }

  return response.json() as Promise<T>;
};

export const markAsRead = async (messageId: number): Promise<void> => {
  return fetchApi<void>(`/Messages/${messageId}/read`, {
    method: 'PATCH',
    expectNoContent: true,
  });
};
