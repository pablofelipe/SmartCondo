import { fetchApi } from './api';
import { User } from '../pages/auth/AuthContext';

export interface LoginResult {
  token: string;
  user: User;
}

export const login = async (
  user: string,
  secret: string,
): Promise<LoginResult> => {
  return fetchApi<LoginResult>('/Auth/login', {
    method: 'POST',
    body: { user, secret },
    authRequired: false,
  });
};

export const forgotPassword = async (
  email: string,
): Promise<{ message: string }> => {
  return fetchApi<{ message: string }>('/ForgotPassword/forgot-password', {
    method: 'POST',
    body: { email },
    authRequired: false,
  });
};

export const resetPassword = async (
  userId: string,
  token: string,
  password: string,
): Promise<{ message: string }> => {
  return fetchApi<{ message: string }>('/ForgotPassword/reset-password', {
    method: 'POST',
    body: { userId, token, password },
    authRequired: false,
  });
};
