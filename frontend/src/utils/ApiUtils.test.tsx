import { generateCorrelationId, getAuthHeaders } from './ApiUtils';

describe('generateCorrelationId', () => {
  it('generates different values on each call', () => {
    const first = generateCorrelationId();
    const second = generateCorrelationId();

    expect(first).not.toEqual(second);
  });
});

describe('getAuthHeaders', () => {
  afterEach(() => {
    localStorage.clear();
  });

  it('includes X-Correlation-Id even without a token', () => {
    const headers = getAuthHeaders();

    expect(headers['X-Correlation-Id']).toBeTruthy();
  });

  it('includes X-Correlation-Id alongside Authorization when a token exists', () => {
    localStorage.setItem('token', 'fake-token');

    const headers = getAuthHeaders();

    expect(headers.Authorization).toBe('Bearer fake-token');
    expect(headers['X-Correlation-Id']).toBeTruthy();
  });

  it('generates a new X-Correlation-Id on each call', () => {
    const first = getAuthHeaders()['X-Correlation-Id'];
    const second = getAuthHeaders()['X-Correlation-Id'];

    expect(first).not.toEqual(second);
  });
});
