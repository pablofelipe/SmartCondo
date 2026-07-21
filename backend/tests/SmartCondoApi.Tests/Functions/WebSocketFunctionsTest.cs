using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using SmartCondoApi.Functions;
using SmartCondoApi.Infra;
using SmartCondoApi.Models;
using SmartCondoApi.Tests.Helpers;

namespace SmartCondoApi.Tests.Functions
{
    [TestClass]
    public class WebSocketFunctionsTest
    {
        private ServiceProvider _serviceProvider = null!;
        private WebSocketFunctions _functions = null!;

        [TestInitialize]
        public void Initialize()
        {
            var services = new ServiceCollection();
            var databaseName = $"webSocketFunctionsTest_{Guid.NewGuid()}";

            services.AddDbContext<SmartCondoContext>(options =>
                options.UseInMemoryDatabase(databaseName));

            services.AddSingleton(TestHelper.CreateConfiguration());
            services.AddScoped<WebSocketFunctions>();

            _serviceProvider = services.BuildServiceProvider();
            _functions = _serviceProvider.GetRequiredService<WebSocketFunctions>();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _serviceProvider.Dispose();
        }

        private static ILambdaContext FakeContext()
        {
            var context = new Mock<ILambdaContext>();
            context.Setup(c => c.Logger).Returns(Mock.Of<ILambdaLogger>());
            return context.Object;
        }

        private static APIGatewayProxyRequest RequestWithQuery(Dictionary<string, string>? query)
        {
            return new APIGatewayProxyRequest
            {
                QueryStringParameters = query,
                RequestContext = new APIGatewayProxyRequest.ProxyRequestContext
                {
                    ConnectionId = "connection-1"
                }
            };
        }

        private static string ValidToken(string subject = "42", DateTime? expires = null)
        {
            var configuration = TestHelper.CreateConfiguration();
            var tokenHandler = new TokenHandler(configuration);
            return tokenHandler.Generate(subject, "resident@example.com", "Resident", expires ?? DateTime.UtcNow.AddMinutes(5));
        }

        [TestMethod]
        public async Task ConnectHandler_ValidToken_CreatesConnectionAndReturns200()
        {
            var request = RequestWithQuery(new Dictionary<string, string> { ["token"] = ValidToken("42") });

            var response = await _functions.ConnectHandler(request, FakeContext());

            Assert.AreEqual(200, response.StatusCode);

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SmartCondoContext>();
            var connection = await context.WebSocketConnections.FirstOrDefaultAsync(c => c.ConnectionId == "connection-1");

            Assert.IsNotNull(connection);
            Assert.AreEqual(42, connection.UserId);
        }

        [TestMethod]
        public async Task ConnectHandler_MissingToken_Returns401()
        {
            var request = RequestWithQuery(new Dictionary<string, string>());

            var response = await _functions.ConnectHandler(request, FakeContext());

            Assert.AreEqual(401, response.StatusCode);
        }

        [TestMethod]
        public async Task ConnectHandler_NoQueryString_Returns401()
        {
            var request = RequestWithQuery(null);

            var response = await _functions.ConnectHandler(request, FakeContext());

            Assert.AreEqual(401, response.StatusCode);
        }

        [TestMethod]
        public async Task ConnectHandler_TamperedToken_Returns401()
        {
            var token = ValidToken("42");
            var tampered = token[..^3] + "xyz";
            var request = RequestWithQuery(new Dictionary<string, string> { ["token"] = tampered });

            var response = await _functions.ConnectHandler(request, FakeContext());

            Assert.AreEqual(401, response.StatusCode);
        }

        [TestMethod]
        public async Task ConnectHandler_ExpiredToken_Returns401()
        {
            var expired = ValidToken("42", DateTime.UtcNow.AddMinutes(-5));
            var request = RequestWithQuery(new Dictionary<string, string> { ["token"] = expired });

            var response = await _functions.ConnectHandler(request, FakeContext());

            Assert.AreEqual(401, response.StatusCode);
        }

        [TestMethod]
        public async Task ConnectHandler_CannotBeImpersonatedByRawUserId()
        {
            // The old vulnerable contract accepted a bare userId query parameter - confirm it's rejected now.
            var request = RequestWithQuery(new Dictionary<string, string> { ["userId"] = "999" });

            var response = await _functions.ConnectHandler(request, FakeContext());

            Assert.AreEqual(401, response.StatusCode);

            using var scope = _serviceProvider.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<SmartCondoContext>();
            var connection = await context.WebSocketConnections.FirstOrDefaultAsync(c => c.UserId == 999);

            Assert.IsNull(connection);
        }
    }
}
