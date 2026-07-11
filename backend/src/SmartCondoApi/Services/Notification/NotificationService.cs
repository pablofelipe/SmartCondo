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
            // Determinar quais usuários devem receber a notificação
            List<long> userIdsToNotify = new();

            switch (message.Scope)
            {
                case MessageScope.Condominium:
                    // Todos os usuários do condomínio
                    userIdsToNotify = await _context.UserProfiles
                        .Where(u => u.CondominiumId == message.CondominiumId)
                        .Select(u => u.Id)
                        .ToListAsync();
                    break;

                case MessageScope.Tower:
                    // Usuários da torre específica
                    userIdsToNotify = await _context.UserProfiles
                        .Where(u => u.CondominiumId == message.CondominiumId && u.TowerId == message.TowerId)
                        .Select(u => u.Id)
                        .ToListAsync();
                    break;

                case MessageScope.Floor:
                    // Usuários do andar específico
                    userIdsToNotify = await _context.UserProfiles
                        .Where(u => u.CondominiumId == message.CondominiumId &&
                                   u.TowerId == message.TowerId &&
                                   u.FloorNumber == message.FloorId)
                        .Select(u => u.Id)
                        .ToListAsync();
                    break;

                case MessageScope.Individual:
                    // Usuário específico
                    if (message.RecipientUserId.HasValue)
                    {
                        userIdsToNotify.Add(message.RecipientUserId.Value);
                    }
                    break;
            }

            // Notificar cada usuário via WebSocket
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
