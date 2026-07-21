using System.Net.WebSockets;

namespace SmartCondoApi.Infra
{
    // Accepts native WebSocket upgrades for the container-hosted notification path. Mirrors the
    // Lambda ConnectFunction's contract (a validated JWT travels as a "token" query parameter,
    // since a browser WebSocket handshake can't carry an Authorization header) but keeps the
    // connection itself in memory via WebSocketConnectionRegistry instead of writing an opaque
    // connection id to the database for a separate process to look up later.
    public static class NativeWebSocketEndpoint
    {
        public const string Path = "/ws";

        public static void Map(IApplicationBuilder app)
        {
            app.Map(Path, wsApp => wsApp.Run(HandleAsync));
        }

        private static async Task HandleAsync(HttpContext context)
        {
            if (!context.WebSockets.IsWebSocketRequest)
            {
                context.Response.StatusCode = StatusCodes.Status400BadRequest;
                return;
            }

            var configuration = context.RequestServices.GetRequiredService<IConfiguration>();
            var token = context.Request.Query["token"].ToString();

            if (!WebSocketTokenValidator.TryGetUserId(token, configuration, out var userId))
            {
                context.Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            var registry = context.RequestServices.GetRequiredService<WebSocketConnectionRegistry>();

            using var socket = await context.WebSockets.AcceptWebSocketAsync();
            var connectionId = registry.Add(userId, socket);

            try
            {
                var buffer = new byte[4096];
                while (socket.State == WebSocketState.Open)
                {
                    var result = await socket.ReceiveAsync(new ArraySegment<byte>(buffer), CancellationToken.None);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        await socket.CloseAsync(WebSocketCloseStatus.NormalClosure, "Closing", CancellationToken.None);
                    }
                }
            }
            finally
            {
                registry.Remove(connectionId);
            }
        }
    }
}
