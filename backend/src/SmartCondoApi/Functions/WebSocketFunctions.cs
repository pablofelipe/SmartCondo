using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SmartCondoApi.Models;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

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
                    !TryGetUserIdFromToken(token, out var userId))
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

        private bool TryGetUserIdFromToken(string? token, out long userId)
        {
            userId = 0;

            if (string.IsNullOrEmpty(token))
            {
                return false;
            }

            var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? _configuration["Jwt:Key"];

            if (string.IsNullOrEmpty(jwtKey))
            {
                return false;
            }

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = _configuration["Jwt:Issuer"],
                ValidAudience = _configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(jwtKey))
            };

            try
            {
                var principal = new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out _);
                var subject = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                return !string.IsNullOrEmpty(subject) && long.TryParse(subject, out userId);
            }
            catch (SecurityTokenException)
            {
                return false;
            }
        }
    }
}