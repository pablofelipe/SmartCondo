using System.Text.Json;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartCondoApi.Exceptions;
using SmartCondoApi.Infra;

namespace SmartCondoApi.Tests.Infra
{
    [TestClass]
    public class ErrorHandlingMiddlewareTest
    {
        private static async Task<TestServer> BuildServerAsync(Action<HttpContext> throwing)
        {
            var host = await new HostBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder.UseTestServer();
                    webBuilder.ConfigureServices(services => services.AddLogging());
                    webBuilder.Configure(app =>
                    {
                        app.UseMiddleware<ErrorHandlingMiddleware>();
                        app.Run(context =>
                        {
                            throwing(context);
                            return Task.CompletedTask;
                        });
                    });
                })
                .StartAsync();

            return host.GetTestServer();
        }

        private static async Task<string?> GetMessageAsync(TestServer server)
        {
            var response = await server.CreateClient().GetAsync("/anything");
            var body = await response.Content.ReadAsStringAsync();
            using var json = JsonDocument.Parse(body);
            return json.RootElement.GetProperty("message").GetString();
        }

        [TestMethod]
        public async Task MessageNotFoundException_ReturnsMessageBody()
        {
            var server = await BuildServerAsync(_ => throw new MessageNotFoundException("not found"));

            var message = await GetMessageAsync(server);

            Assert.AreEqual("not found", message);
        }

        [TestMethod]
        public async Task UnauthorizedAccessException_ReturnsMessageBody()
        {
            var server = await BuildServerAsync(_ => throw new UnauthorizedAccessException("forbidden"));

            var message = await GetMessageAsync(server);

            Assert.AreEqual("forbidden", message);
        }

        [TestMethod]
        public async Task UnhandledException_ReturnsGenericMessageBody()
        {
            var server = await BuildServerAsync(_ => throw new InvalidOperationException("boom"));

            var message = await GetMessageAsync(server);

            Assert.AreEqual("An unexpected error occurred", message);
        }
    }
}
