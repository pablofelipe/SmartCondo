using System.Collections.Concurrent;
using System.Net.WebSockets;

namespace SmartCondoApi.Infra
{
    // In-process registry of live WebSocket connections for the container-hosted notification
    // path - the direct equivalent of the WebSocketConnections table the Lambda path uses, except
    // the connection itself (not just an opaque id) can live here because Kestrel, unlike Lambda,
    // keeps the process running between messages. Singleton: one registry per running instance.
    public class WebSocketConnectionRegistry
    {
        private readonly ConcurrentDictionary<Guid, (long UserId, WebSocket Socket)> _connections = new();

        public Guid Add(long userId, WebSocket socket)
        {
            var connectionId = Guid.NewGuid();
            _connections[connectionId] = (userId, socket);
            return connectionId;
        }

        public void Remove(Guid connectionId) => _connections.TryRemove(connectionId, out _);

        public IReadOnlyCollection<WebSocket> GetSockets(long userId) =>
            _connections.Values
                .Where(c => c.UserId == userId)
                .Select(c => c.Socket)
                .ToList();
    }
}
