export const generateCorrelationId = (): string =>
  `${Date.now().toString(36)}-${Math.random().toString(36).slice(2)}`;

export const getAuthHeaders = (): Record<string, string> => {
  const token = localStorage.getItem('token');
  const correlationId = generateCorrelationId();

  if (!token) {
    console.error('Token não encontrado. Faça login novamente.');
    return {
      'Content-Type': 'application/json',
      'X-Correlation-Id': correlationId,
    };
  }

  return {
    Authorization: `Bearer ${token}`,
    'Content-Type': 'application/json',
    'X-Correlation-Id': correlationId,
  };
};
