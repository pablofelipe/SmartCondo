using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Microsoft.EntityFrameworkCore;
using SmartCondoApi.Models;

namespace SmartCondoApi.Functions
{
    public class WebSocketFunctions
    {
        private readonly IServiceProvider _serviceProvider;

        public WebSocketFunctions(IServiceProvider serviceProvider)
        {
            _serviceProvider = serviceProvider;
        }

        public async Task<APIGatewayProxyResponse> ConnectHandler(
            APIGatewayProxyRequest request, ILambdaContext context)
        {
            using var scope = _serviceProvider.CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<SmartCondoContext>();

            try
            {
                var connectionId = request.RequestContext.ConnectionId;

                if (!request.QueryStringParameters.TryGetValue("userId", out var userIdStr) ||
                    !long.TryParse(userIdStr, out var userId))
                {
                    return new APIGatewayProxyResponse { StatusCode = 400 };
                }

                // Remover conexões antigas do mesmo usuário
                var existingConnections = await dbContext.WebSocketConnections
                    .Where(c => c.UserId == userId)
                    .ToListAsync();

                dbContext.WebSocketConnections.RemoveRange(existingConnections);

                // Adicionar nova conexão
                dbContext.WebSocketConnections.Add(new WebSocketConnection
                {
                    ConnectionId = connectionId,
                    UserId = userId,
                    ConnectedAt = DateTime.UtcNow,
                    IsActive = true
                });

                await dbContext.SaveChangesAsync();

                return new APIGatewayProxyResponse { StatusCode = 200 };
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error in ConnectHandler: {ex.Message}");
                return new APIGatewayProxyResponse { StatusCode = 500 };
            }
        }

        public async Task<APIGatewayProxyResponse> DisconnectHandler(
            APIGatewayProxyRequest request, ILambdaContext context)
        {
            using var scope = _serviceProvider.CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<SmartCondoContext>();

            try
            {
                var connectionId = request.RequestContext.ConnectionId;

                var connection = await dbContext.WebSocketConnections
                    .FirstOrDefaultAsync(c => c.ConnectionId == connectionId);

                if (connection != null)
                {
                    dbContext.WebSocketConnections.Remove(connection);
                    await dbContext.SaveChangesAsync();
                }

                return new APIGatewayProxyResponse { StatusCode = 200 };
            }
            catch (Exception ex)
            {
                context.Logger.LogError($"Error in DisconnectHandler: {ex.Message}");
                return new APIGatewayProxyResponse { StatusCode = 500 };
            }
        }
    }
}