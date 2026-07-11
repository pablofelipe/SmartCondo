export const getAuthHeaders = (): Record<string, string> => {
  const token = localStorage.getItem('token');

  if (!token) {
    console.error('Token não encontrado. Faça login novamente.');
    return {
      'Content-Type': 'application/json',
    };
  }

  return {
    Authorization: `Bearer ${token}`,
    'Content-Type': 'application/json',
  };
};
