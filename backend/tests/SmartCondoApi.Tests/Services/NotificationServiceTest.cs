using Amazon.ApiGatewayManagementApi;
using Amazon.ApiGatewayManagementApi.Model;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using SmartCondoApi.Models;
using SmartCondoApi.Services.Notification;

namespace SmartCondoApi.Tests.Services
{
    [TestClass]
    public class NotificationServiceTest
    {
        private static SmartCondoContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<SmartCondoContext>()
                .UseInMemoryDatabase($"notificationServiceTest_{Guid.NewGuid()}")
                .Options;

            return new SmartCondoContext(options);
        }

        [TestMethod]
        public async Task NotifyNewMessageAsync_WhenPostToConnectionFails_LogsViaILogger()
        {
            using var context = CreateContext();
            context.WebSocketConnections.Add(new WebSocketConnection
            {
                ConnectionId = "conn-1",
                UserId = 1,
                IsActive = true,
                ConnectedAt = DateTime.UtcNow
            });
            await context.SaveChangesAsync();

            var gatewayMock = new Mock<IAmazonApiGatewayManagementApi>();
            gatewayMock
                .Setup(g => g.PostToConnectionAsync(It.IsAny<PostToConnectionRequest>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("gateway unavailable"));

            var loggerMock = new Mock<ILogger<NotificationService>>();

            var service = new NotificationService(context, gatewayMock.Object, loggerMock.Object);

            var message = new Message
            {
                Id = 1,
                Content = "hello",
                SentDate = DateTime.UtcNow,
                SenderId = 2,
                Scope = MessageScope.Individual,
                CondominiumId = 1,
                RecipientUserId = 1
            };

            await service.NotifyNewMessageAsync(message);

            loggerMock.Verify(
                l => l.Log(
                    LogLevel.Error,
                    It.IsAny<EventId>(),
                    It.IsAny<It.IsAnyType>(),
                    It.IsAny<InvalidOperationException>(),
                    It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
                Times.Once);
        }
    }
}
