using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SmartCondoApi.Controllers;
using SmartCondoApi.Models;

namespace SmartCondoApi.Tests.Controllers
{
    [TestClass]
    public class MigrationControllerTest
    {
        private const string AuthKey = "correct-migration-key-12345";

        private static MigrationController BuildController()
        {
            var services = new ServiceCollection();
            services.AddDbContext<SmartCondoContext>(options =>
                options.UseInMemoryDatabase($"migrationControllerTest_{Guid.NewGuid()}"));

            var serviceProvider = services.BuildServiceProvider();

            var configuration = new ConfigurationBuilder()
                .AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["MIGRATION_AUTH_KEY"] = AuthKey
                })
                .Build();

            return new MigrationController(serviceProvider, configuration, Mock.Of<ILogger<MigrationController>>())
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext()
                }
            };
        }

        private static void WithHeader(MigrationController controller, string key)
        {
            controller.ControllerContext.HttpContext.Request.Headers["X-Migration-Auth"] = key;
        }

        [TestMethod]
        public async Task Migrate_WrongKeySameLength_ReturnsUnauthorized()
        {
            var controller = BuildController();
            WithHeader(controller, new string('x', AuthKey.Length));

            var result = await controller.Migrate();

            Assert.IsInstanceOfType(result, typeof(UnauthorizedResult));
        }

        [TestMethod]
        public async Task Migrate_WrongKeyDifferentLength_ReturnsUnauthorized()
        {
            var controller = BuildController();
            WithHeader(controller, "short");

            var result = await controller.Migrate();

            Assert.IsInstanceOfType(result, typeof(UnauthorizedResult));
        }

        [TestMethod]
        public async Task Migrate_MissingKey_ReturnsUnauthorized()
        {
            var controller = BuildController();

            var result = await controller.Migrate();

            Assert.IsInstanceOfType(result, typeof(UnauthorizedResult));
        }

        [TestMethod]
        public async Task Migrate_CorrectKey_PassesAuthorizationCheck()
        {
            var controller = BuildController();
            WithHeader(controller, AuthKey);

            var result = await controller.Migrate();

            // The InMemory provider has no migration support, so this fails past the auth check -
            // proving the key comparison itself succeeded, not that the whole migration ran.
            Assert.IsNotInstanceOfType(result, typeof(UnauthorizedResult));
        }
    }
}
