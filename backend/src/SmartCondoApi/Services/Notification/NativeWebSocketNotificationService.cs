using SmartCondoApi.Infra;
using SmartCondoApi.Models;
using System.Net.WebSockets;
using System.Text;
using System.Text.Json;

namespace SmartCondoApi.Services.Notification
{
    // Container-hosted counterpart to the AWS-API-Gateway-based NotificationService: pushes
    // directly over in-process WebSocket connections instead of calling out to a cloud SDK,
    // since Kestrel (unlike Lambda) can hold a persistent connection itself.
    public class NativeWebSocketNotificationService(SmartCondoContext _context, WebSocketConnectionRegistry _registry, ILogger<NativeWebSocketNotificationService> _logger) : INotificationService
    {
        public async Task NotifyNewMessageAsync(Models.Message message)
        {
            var userIdsToNotify = await MessageRecipientResolver.ResolveUserIdsAsync(_context, message);

            foreach (var userId in userIdsToNotify)
            {
                await NotifyUserAsync(userId, message);
            }
        }

        private async Task NotifyUserAsync(long userId, Models.Message message)
        {
            var sockets = _registry.GetSockets(userId);
            if (sockets.Count == 0)
                return;

            var payload = JsonSerializer.Serialize(new
            {
                type = "NEW_MESSAGE",
                message = new
                {
                    id = message.Id,
                    content = message.Content,
                    sentDate = message.SentDate,
                    senderId = message.SenderId,
                    scope = message.Scope.ToString()
                }
            });

            var bytes = Encoding.UTF8.GetBytes(payload);

            foreach (var socket in sockets)
            {
                try
                {
                    if (socket.State == WebSocketState.Open)
                    {
                        await socket.SendAsync(bytes, WebSocketMessageType.Text, endOfMessage: true, CancellationToken.None);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Failed to push notification to user {UserId}", userId);
                }
            }
        }
    }
}
