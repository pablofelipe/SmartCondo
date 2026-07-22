import { generateCorrelationId, getAuthHeaders } from './ApiUtils';

describe('generateCorrelationId', () => {
  it('gera valores diferentes a cada chamada', () => {
    const first = generateCorrelationId();
    const second = generateCorrelationId();

    expect(first).not.toEqual(second);
  });
});

describe('getAuthHeaders', () => {
  afterEach(() => {
    localStorage.clear();
  });

  it('inclui X-Correlation-Id mesmo sem token', () => {
    const headers = getAuthHeaders();

    expect(headers['X-Correlation-Id']).toBeTruthy();
  });

  it('inclui X-Correlation-Id junto com o Authorization quando há token', () => {
    localStorage.setItem('token', 'fake-token');

    const headers = getAuthHeaders();

    expect(headers.Authorization).toBe('Bearer fake-token');
    expect(headers['X-Correlation-Id']).toBeTruthy();
  });

  it('gera um X-Correlation-Id novo a cada chamada', () => {
    const first = getAuthHeaders()['X-Correlation-Id'];
    const second = getAuthHeaders()['X-Correlation-Id'];

    expect(first).not.toEqual(second);
  });
});
