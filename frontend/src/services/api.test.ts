import { fetchApi } from './api';

const jsonResponse = (body: unknown, init: ResponseInit = { status: 200 }) =>
  new Response(JSON.stringify(body), {
    headers: { 'Content-Type': 'application/json' },
    ...init,
  });

describe('fetchApi', () => {
  const originalFetch = global.fetch;

  beforeEach(() => {
    global.fetch = jest.fn();
    localStorage.clear();
  });

  afterEach(() => {
    global.fetch = originalFetch;
  });

  it('sends the bearer token by default when one is stored', async () => {
    localStorage.setItem('token', 'stored-token');
    (global.fetch as jest.Mock).mockResolvedValue(jsonResponse({ ok: true }));

    await fetchApi('/UserProfile/1');

    const [, requestInit] = (global.fetch as jest.Mock).mock.calls[0];
    expect(requestInit.headers.Authorization).toBe('Bearer stored-token');
  });

  it('omits the Authorization header when authRequired is false', async () => {
    localStorage.setItem('token', 'stored-token');
    (global.fetch as jest.Mock).mockResolvedValue(
      jsonResponse({ token: 'x', user: {} }),
    );

    await fetchApi('/Auth/login', {
      method: 'POST',
      body: { user: 'a', secret: 'b' },
      authRequired: false,
    });

    const [, requestInit] = (global.fetch as jest.Mock).mock.calls[0];
    expect(requestInit.headers.Authorization).toBeUndefined();
  });

  it('throws with the backend-provided message on a failed request', async () => {
    (global.fetch as jest.Mock).mockResolvedValue(
      jsonResponse({ message: 'Invalid credentials.' }, { status: 400 }),
    );

    await expect(
      fetchApi('/Auth/login', {
        method: 'POST',
        body: { user: 'a', secret: 'b' },
        authRequired: false,
      }),
    ).rejects.toThrow('Invalid credentials.');
  });

  it('joins ASP.NET Core model-validation errors into a single message', async () => {
    (global.fetch as jest.Mock).mockResolvedValue(
      jsonResponse(
        {
          errors: {
            Email: ['Email is required.'],
            Name: ['Name must have at least 3 characters.'],
          },
        },
        { status: 400 },
      ),
    );

    await expect(fetchApi('/UserProfile', { method: 'POST' })).rejects.toThrow(
      'Email is required.\nName must have at least 3 characters.',
    );
  });

  it('falls back to a generic message when the error body has none', async () => {
    (global.fetch as jest.Mock).mockResolvedValue(
      jsonResponse({}, { status: 500 }),
    );

    await expect(fetchApi('/dashboard/stats')).rejects.toThrow(
      'Request failed',
    );
  });

  it('returns undefined without parsing a body when expectNoContent is set', async () => {
    (global.fetch as jest.Mock).mockResolvedValue(
      new Response(null, { status: 200 }),
    );

    await expect(
      fetchApi('/UserProfile/1', { method: 'DELETE', expectNoContent: true }),
    ).resolves.toBeUndefined();
  });
});
