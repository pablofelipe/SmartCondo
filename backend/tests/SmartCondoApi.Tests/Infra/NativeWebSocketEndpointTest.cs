using System.Net.WebSockets;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartCondoApi.Infra;
using SmartCondoApi.Tests.Helpers;

namespace SmartCondoApi.Tests.Infra
{
    [TestClass]
    public class NativeWebSocketEndpointTest
    {
        private static string ValidToken(string subject = "42")
        {
            var configuration = TestHelper.CreateConfiguration();
            var tokenHandler = new TokenHandler(configuration);
            return tokenHandler.Generate(subject, "resident@example.com", "Resident", DateTime.UtcNow.AddMinutes(5));
        }

        private static async Task<(TestServer Server, WebSocketConnectionRegistry Registry)> BuildServerAsync()
        {
            var registry = new WebSocketConnectionRegistry();

            var host = await new HostBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder.UseTestServer();
                    webBuilder.ConfigureServices(services =>
                    {
                        services.AddSingleton(TestHelper.CreateConfiguration());
                        services.AddSingleton(registry);
                    });
                    webBuilder.Configure(app =>
                    {
                        app.UseWebSockets();
                        NativeWebSocketEndpoint.Map(app);
                    });
                })
                .StartAsync();

            return (host.GetTestServer(), registry);
        }

        [TestMethod]
        public async Task Connect_ValidToken_RegistersTheConnectionForThatUser()
        {
            var (server, registry) = await BuildServerAsync();
            var client = server.CreateWebSocketClient();

            using var socket = await client.ConnectAsync(
                new Uri($"ws://localhost{NativeWebSocketEndpoint.Path}?token={ValidToken("42")}"),
                CancellationToken.None);

            Assert.AreEqual(WebSocketState.Open, socket.State);

            // The client-side handshake completes slightly before the server finishes running
            // HandleAsync's registry.Add - poll briefly instead of asserting immediately.
            for (var i = 0; i < 20 && registry.GetSockets(42).Count == 0; i++)
                await Task.Delay(25);

            Assert.AreEqual(1, registry.GetSockets(42).Count);
        }

        [TestMethod]
        public async Task Connect_MissingToken_RejectsTheHandshake()
        {
            var (server, _) = await BuildServerAsync();
            var client = server.CreateWebSocketClient();

            // TestServer's WebSocketClient surfaces a rejected (non-101) handshake as
            // InvalidOperationException, not WebSocketException as a real ClientWebSocket would.
            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
                client.ConnectAsync(new Uri($"ws://localhost{NativeWebSocketEndpoint.Path}"), CancellationToken.None));
        }

        [TestMethod]
        public async Task Connect_TamperedToken_RejectsTheHandshake()
        {
            var (server, _) = await BuildServerAsync();
            var client = server.CreateWebSocketClient();
            var tampered = ValidToken("42")[..^3] + "xyz";

            await Assert.ThrowsExceptionAsync<InvalidOperationException>(() =>
                client.ConnectAsync(new Uri($"ws://localhost{NativeWebSocketEndpoint.Path}?token={tampered}"), CancellationToken.None));
        }

        [TestMethod]
        public async Task Close_RemovesTheConnectionFromTheRegistry()
        {
            var (server, registry) = await BuildServerAsync();
            var client = server.CreateWebSocketClient();

            var socket = await client.ConnectAsync(
                new Uri($"ws://localhost{NativeWebSocketEndpoint.Path}?token={ValidToken("42")}"),
                CancellationToken.None);

            for (var i = 0; i < 20 && registry.GetSockets(42).Count == 0; i++)
                await Task.Delay(25);

            Assert.AreEqual(1, registry.GetSockets(42).Count);

            await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);

            // Give the server-side receive loop a moment to observe the close frame and deregister.
            for (var i = 0; i < 20 && registry.GetSockets(42).Count > 0; i++)
                await Task.Delay(25);

            Assert.AreEqual(0, registry.GetSockets(42).Count);
        }
    }
}
