using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SmartCondoApi.Infra;
using SmartCondoApi.Models;

namespace SmartCondoApi.Tests.Infra
{
    [TestClass]
    public class DatabaseHealthCheckTest
    {
        private static SmartCondoContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<SmartCondoContext>()
                .UseInMemoryDatabase($"databaseHealthCheckTest_{Guid.NewGuid()}")
                .Options;

            return new SmartCondoContext(options);
        }

        [TestMethod]
        public async Task CheckHealthAsync_ConnectableDatabase_ReturnsHealthy()
        {
            using var context = CreateContext();
            var check = new DatabaseHealthCheck(context);

            var result = await check.CheckHealthAsync(new HealthCheckContext());

            Assert.AreEqual(HealthStatus.Healthy, result.Status);
        }

        [TestMethod]
        public async Task CheckHealthAsync_ContextThrows_ReturnsUnhealthy()
        {
            var context = CreateContext();
            context.Dispose(); // Forces Database.CanConnectAsync to throw ObjectDisposedException.
            var check = new DatabaseHealthCheck(context);

            var result = await check.CheckHealthAsync(new HealthCheckContext());

            Assert.AreEqual(HealthStatus.Unhealthy, result.Status);
            Assert.IsNotNull(result.Exception);
        }
    }
}
