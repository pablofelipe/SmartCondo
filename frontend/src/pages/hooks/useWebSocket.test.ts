import { renderHook } from '@testing-library/react';
import { useWebSocket } from './useWebSocket';

class MockWebSocket {
  static instances: MockWebSocket[] = [];
  url: string;
  onopen: (() => void) | null = null;
  onmessage: ((event: MessageEvent) => void) | null = null;
  onclose: (() => void) | null = null;
  onerror: ((event: Event) => void) | null = null;
  close = jest.fn();

  constructor(url: string) {
    this.url = url;
    MockWebSocket.instances.push(this);
  }
}

describe('useWebSocket', () => {
  const OriginalWebSocket = global.WebSocket;

  beforeEach(() => {
    MockWebSocket.instances = [];
    (global as unknown as { WebSocket: unknown }).WebSocket = MockWebSocket;
    localStorage.clear();
  });

  afterEach(() => {
    (global as unknown as { WebSocket: unknown }).WebSocket =
      OriginalWebSocket;
  });

  it('does not reopen the connection when the onMessage callback identity changes across renders', () => {
    const onMessage1 = jest.fn();
    const { rerender } = renderHook(
      ({ onMessage }) => useWebSocket(1, onMessage),
      { initialProps: { onMessage: onMessage1 } },
    );

    expect(MockWebSocket.instances).toHaveLength(1);

    const onMessage2 = jest.fn();
    rerender({ onMessage: onMessage2 });

    expect(MockWebSocket.instances).toHaveLength(1);
  });

  it('routes incoming messages to the latest onMessage callback after a rerender', () => {
    const onMessage1 = jest.fn();
    const { rerender } = renderHook(
      ({ onMessage }) => useWebSocket(1, onMessage),
      { initialProps: { onMessage: onMessage1 } },
    );

    const onMessage2 = jest.fn();
    rerender({ onMessage: onMessage2 });

    const instance = MockWebSocket.instances[0];
    instance.onmessage?.({
      data: JSON.stringify({ type: 'NEW_MESSAGE' }),
    } as MessageEvent);

    expect(onMessage1).not.toHaveBeenCalled();
    expect(onMessage2).toHaveBeenCalledWith({ type: 'NEW_MESSAGE' });
  });

  it('does not connect when userId is null', () => {
    renderHook(() => useWebSocket(null, jest.fn()));

    expect(MockWebSocket.instances).toHaveLength(0);
  });
});
