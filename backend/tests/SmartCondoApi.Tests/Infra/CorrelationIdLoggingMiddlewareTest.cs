using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using SmartCondoApi.Infra;

namespace SmartCondoApi.Tests.Infra
{
    [TestClass]
    public class CorrelationIdLoggingMiddlewareTest
    {
        private static async Task<TestServer> BuildServerAsync()
        {
            var host = await new HostBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder.UseTestServer();
                    webBuilder.Configure(app =>
                    {
                        app.UseMiddleware<CorrelationIdLoggingMiddleware>();
                        app.Run(context => context.Response.WriteAsync("ok"));
                    });
                })
                .StartAsync();

            return host.GetTestServer();
        }

        [TestMethod]
        public async Task Response_IncludesCorrelationIdHeader()
        {
            var server = await BuildServerAsync();
            var client = server.CreateClient();

            var response = await client.GetAsync("/anything");

            Assert.IsTrue(response.Headers.Contains("X-Correlation-Id"));
            var value = response.Headers.GetValues("X-Correlation-Id").Single();
            Assert.IsFalse(string.IsNullOrWhiteSpace(value));
        }

        [TestMethod]
        public async Task EachRequest_GetsADifferentCorrelationId()
        {
            var server = await BuildServerAsync();
            var client = server.CreateClient();

            var first = await client.GetAsync("/anything");
            var second = await client.GetAsync("/anything");

            var firstId = first.Headers.GetValues("X-Correlation-Id").Single();
            var secondId = second.Headers.GetValues("X-Correlation-Id").Single();

            Assert.AreNotEqual(firstId, secondId);
        }
    }
}
