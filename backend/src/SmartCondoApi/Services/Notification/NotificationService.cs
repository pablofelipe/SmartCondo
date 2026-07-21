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

        public NotificationService(SmartCondoContext context, IAmazonApiGatewayManagementApi apiGateway)
        {
            _context = context;
            _apiGateway = apiGateway;
        }

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
            var connections = await _context.WebSocketConnections
                .Where(c => c.UserId == userId && c.IsActive)
                .ToListAsync();

            foreach (var connection in connections)
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
                    // Log do erro
                    Console.WriteLine($"Erro ao notificar usuário {userId}: {ex.Message}");
                }
            }

            await _context.SaveChangesAsync();
        }
    }
}
