using Amazon.ApiGatewayManagementApi;
using Amazon.ApiGatewayManagementApi.Model;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Text.Json;
using SmartCondoApi.Models;

namespace SmartCondoApi.Services.Notification
{
    public class NotificationService : INotificationService
    {
        private readonly SmartCondoContext _context;
        private readonly IAmazonApiGatewayManagementApi _apiGateway;
        private readonly ILogger<NotificationService> _logger;

        public NotificationService(SmartCondoContext context, IAmazonApiGatewayManagementApi apiGateway, ILogger<NotificationService> logger)
        {
            _context = context;
            _apiGateway = apiGateway;
            _logger = logger;
        }

        public async Task NotifyNewMessageAsync(Models.Message message)
        {
            var userIdsToNotify = await MessageRecipientResolver.ResolveUserIdsAsync(_context, message);

            var connections = await _context.WebSocketConnections
                .Where(c => userIdsToNotify.Contains(c.UserId) && c.IsActive)
                .ToListAsync();

            foreach (var connection in connections)
            {
                await SendToConnectionAsync(connection, message);
            }

            await _context.SaveChangesAsync();
        }

        private async Task SendToConnectionAsync(WebSocketConnection connection, Models.Message message)
        {
            try
            {
                var notification = new
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
                };

                var stream = new MemoryStream(
                    Encoding.UTF8.GetBytes(JsonSerializer.Serialize(notification)));

                await _apiGateway.PostToConnectionAsync(new PostToConnectionRequest
                {
                    ConnectionId = connection.ConnectionId,
                    Data = stream
                });
            }
            catch (GoneException)
            {
                // Conexão não existe mais, remover do banco
                connection.IsActive = false;
                _context.WebSocketConnections.Update(connection);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao notificar usuário {UserId}", connection.UserId);
            }
        }
    }
}
