using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SmartCondoApi.Infra;
using SmartCondoApi.Models;

namespace SmartCondoApi.Functions
{
    public class WebSocketFunctions
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly IConfiguration _configuration;

        public WebSocketFunctions(IServiceProvider serviceProvider, IConfiguration configuration)
        {
            _serviceProvider = serviceProvider;
            _configuration = configuration;
        }

        public async Task<APIGatewayProxyResponse> ConnectHandler(
            APIGatewayProxyRequest request, ILambdaContext context)
        {
            using var scope = _serviceProvider.CreateScope();
            using var dbContext = scope.ServiceProvider.GetRequiredService<SmartCondoContext>();

            try
            {
                var connectionId = request.RequestContext.ConnectionId;

                // The WebSocket handshake can't carry an Authorization header from a browser client,
                // so the JWT travels as a query string parameter instead. The connecting user's identity
                // must come from this validated token, never from a caller-supplied userId - trusting a
                // raw userId query parameter would let anyone subscribe to any other user's notifications.
                if (request.QueryStringParameters == null ||
                    !request.QueryStringParameters.TryGetValue("token", out var token) ||
                    !WebSocketTokenValidator.TryGetUserId(token, _configuration, out var userId))
                {
                    return new APIGatewayProxyResponse { StatusCode = 401 };
                }

                // Remove any older connections for the same user
                var existingConnections = await dbContext.WebSocketConnections
                    .Where(c => c.UserId == userId)
                    .ToListAsync();

                dbContext.WebSocketConnections.RemoveRange(existingConnections);

                // Add the new connection
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