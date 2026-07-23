export const generateCorrelationId = (): string =>
  `${Date.now().toString(36)}-${Math.random().toString(36).slice(2)}`;

// Headers for endpoints that don't require a session (login, forgot/reset password).
// Kept separate from getAuthHeaders so those calls don't trigger its "token missing"
// warning, which is only meaningful when a token was actually expected.
export const getPublicHeaders = (): Record<string, string> => ({
  'Content-Type': 'application/json',
  'X-Correlation-Id': generateCorrelationId(),
});

export const getAuthHeaders = (): Record<string, string> => {
  const token = localStorage.getItem('token');

  if (!token) {
    console.error('Token not found. Please log in again.');
    return getPublicHeaders();
  }

  return {
    ...getPublicHeaders(),
    Authorization: `Bearer ${token}`,
  };
};
