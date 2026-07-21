using Moq;
using SmartCondoApi.Infra;
using System.Net.WebSockets;

namespace SmartCondoApi.Tests.Infra
{
    [TestClass]
    public class WebSocketConnectionRegistryTest
    {
        [TestMethod]
        public void GetSockets_NoConnections_ReturnsEmpty()
        {
            var registry = new WebSocketConnectionRegistry();

            var sockets = registry.GetSockets(userId: 1);

            Assert.AreEqual(0, sockets.Count);
        }

        [TestMethod]
        public void Add_ThenGetSockets_ReturnsTheSocketForThatUser()
        {
            var registry = new WebSocketConnectionRegistry();
            var socket = Mock.Of<WebSocket>();

            registry.Add(userId: 1, socket);

            var sockets = registry.GetSockets(userId: 1);
            Assert.AreEqual(1, sockets.Count);
            Assert.AreSame(socket, sockets.Single());
        }

        [TestMethod]
        public void GetSockets_DoesNotReturnConnectionsForOtherUsers()
        {
            var registry = new WebSocketConnectionRegistry();
            registry.Add(userId: 1, Mock.Of<WebSocket>());

            var sockets = registry.GetSockets(userId: 2);

            Assert.AreEqual(0, sockets.Count);
        }

        [TestMethod]
        public void GetSockets_SameUserMultipleConnections_ReturnsAll()
        {
            var registry = new WebSocketConnectionRegistry();
            registry.Add(userId: 1, Mock.Of<WebSocket>());
            registry.Add(userId: 1, Mock.Of<WebSocket>());

            var sockets = registry.GetSockets(userId: 1);

            Assert.AreEqual(2, sockets.Count);
        }

        [TestMethod]
        public void Remove_StopsTheConnectionFromBeingReturned()
        {
            var registry = new WebSocketConnectionRegistry();
            var connectionId = registry.Add(userId: 1, Mock.Of<WebSocket>());

            registry.Remove(connectionId);

            Assert.AreEqual(0, registry.GetSockets(userId: 1).Count);
        }
    }
}
