using Microsoft.EntityFrameworkCore;
using SmartCondoApi.Models;
using Testcontainers.PostgreSql;

namespace SmartCondoApi.Tests.Integration
{
    // Runs against a real PostgreSQL instance (via Testcontainers), not the EF Core
    // InMemory provider used by the rest of the suite. InMemory never applies real
    // migrations and silently ignores configured delete-behavior/constraints, so it
    // cannot catch the kind of gap covered here. Requires a local Docker daemon.
    [TestClass]
    public class PostgresIntegrationTest
    {
        private static PostgreSqlContainer _container = null!;
        private static SmartCondoContext _context = null!;

        [ClassInitialize]
        public static async Task ClassInitialize(TestContext _)
        {
            _container = new PostgreSqlBuilder("postgres:16-alpine")
                .Build();
            await _container.StartAsync();

            var options = new DbContextOptionsBuilder<SmartCondoContext>()
                .UseNpgsql(_container.GetConnectionString())
                .Options;

            _context = new SmartCondoContext(options);
            await _context.Database.MigrateAsync();
        }

        [ClassCleanup]
        public static async Task ClassCleanup()
        {
            await _context.DisposeAsync();
            await _container.DisposeAsync();
        }

        [TestMethod]
        public async Task Migrations_ApplyCleanlyAgainstRealPostgres()
        {
            var pending = await _context.Database.GetPendingMigrationsAsync();
            var applied = await _context.Database.GetAppliedMigrationsAsync();

            Assert.IsFalse(pending.Any(), "Every migration should already be applied by ClassInitialize.");
            Assert.IsTrue(applied.Any(), "At least one migration should have run.");
        }

        [TestMethod]
        public async Task DeletingAUserProfile_CascadeDeletesItsWebSocketConnections()
        {
            var profile = new UserProfile
            {
                Name = "Cascade Test",
                Address = "1 Main St",
                Phone1 = "555-0100",
                RegistrationNumber = $"REG-{Guid.NewGuid():N}",
                UserTypeId = 1,
            };
            _context.UserProfiles.Add(profile);
            await _context.SaveChangesAsync();

            var connection = new WebSocketConnection
            {
                ConnectionId = Guid.NewGuid().ToString(),
                UserId = profile.Id,
                IsActive = true,
                ConnectedAt = DateTime.UtcNow,
            };
            _context.WebSocketConnections.Add(connection);
            await _context.SaveChangesAsync();

            _context.UserProfiles.Remove(profile);
            await _context.SaveChangesAsync();

            var survivingConnection = await _context.WebSocketConnections
                .FirstOrDefaultAsync(c => c.ConnectionId == connection.ConnectionId);

            Assert.IsNull(survivingConnection, "The cascade delete configured on WebSocketConnection.User should have removed it with its owning profile.");
        }
    }
}
