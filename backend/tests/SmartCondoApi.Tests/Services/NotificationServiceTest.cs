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

        [TestMethod]
        public async Task NotifyNewMessageAsync_CondominiumBroadcastToMultipleUsers_NotifiesEveryActiveConnection()
        {
            using var context = CreateContext();
            context.UserProfiles.AddRange(
                new UserProfile { Id = 1, Name = "A", Address = "-", Phone1 = "-", RegistrationNumber = "1", CondominiumId = 1 },
                new UserProfile { Id = 2, Name = "B", Address = "-", Phone1 = "-", RegistrationNumber = "2", CondominiumId = 1 });
            context.WebSocketConnections.AddRange(
                new WebSocketConnection { ConnectionId = "conn-1", UserId = 1, IsActive = true, ConnectedAt = DateTime.UtcNow },
                new WebSocketConnection { ConnectionId = "conn-2", UserId = 2, IsActive = true, ConnectedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var gatewayMock = new Mock<IAmazonApiGatewayManagementApi>();
            gatewayMock
                .Setup(g => g.PostToConnectionAsync(It.IsAny<PostToConnectionRequest>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PostToConnectionResponse());

            var service = new NotificationService(context, gatewayMock.Object, new Mock<ILogger<NotificationService>>().Object);

            var message = new Message
            {
                Id = 1,
                Content = "hello",
                SentDate = DateTime.UtcNow,
                SenderId = 3,
                Scope = MessageScope.Condominium,
                CondominiumId = 1
            };

            await service.NotifyNewMessageAsync(message);

            gatewayMock.Verify(
                g => g.PostToConnectionAsync(
                    It.Is<PostToConnectionRequest>(r => r.ConnectionId == "conn-1"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
            gatewayMock.Verify(
                g => g.PostToConnectionAsync(
                    It.Is<PostToConnectionRequest>(r => r.ConnectionId == "conn-2"),
                    It.IsAny<CancellationToken>()),
                Times.Once);
        }

        [TestMethod]
        public async Task NotifyNewMessageAsync_WhenConnectionIsGone_DeactivatesItWithoutAffectingOtherUsersConnections()
        {
            using var context = CreateContext();
            context.UserProfiles.AddRange(
                new UserProfile { Id = 1, Name = "A", Address = "-", Phone1 = "-", RegistrationNumber = "1", CondominiumId = 1 },
                new UserProfile { Id = 2, Name = "B", Address = "-", Phone1 = "-", RegistrationNumber = "2", CondominiumId = 1 });
            context.WebSocketConnections.AddRange(
                new WebSocketConnection { ConnectionId = "gone-conn", UserId = 1, IsActive = true, ConnectedAt = DateTime.UtcNow },
                new WebSocketConnection { ConnectionId = "healthy-conn", UserId = 2, IsActive = true, ConnectedAt = DateTime.UtcNow });
            await context.SaveChangesAsync();

            var gatewayMock = new Mock<IAmazonApiGatewayManagementApi>();
            gatewayMock
                .Setup(g => g.PostToConnectionAsync(It.Is<PostToConnectionRequest>(r => r.ConnectionId == "gone-conn"), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new GoneException("gone"));
            gatewayMock
                .Setup(g => g.PostToConnectionAsync(It.Is<PostToConnectionRequest>(r => r.ConnectionId == "healthy-conn"), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new PostToConnectionResponse());

            var service = new NotificationService(context, gatewayMock.Object, new Mock<ILogger<NotificationService>>().Object);

            var message = new Message
            {
                Id = 1,
                Content = "hello",
                SentDate = DateTime.UtcNow,
                SenderId = 3,
                Scope = MessageScope.Condominium,
                CondominiumId = 1
            };

            await service.NotifyNewMessageAsync(message);

            var goneConnection = await context.WebSocketConnections.FindAsync("gone-conn");
            var healthyConnection = await context.WebSocketConnections.FindAsync("healthy-conn");
            Assert.IsFalse(goneConnection!.IsActive);
            Assert.IsTrue(healthyConnection!.IsActive);
        }
    }
}
