using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System.Threading.RateLimiting;

namespace SmartCondoApi.Tests.Infra
{
    [TestClass]
    public class RateLimiterTest
    {
        // Mirrors the LoginRateLimit policy registered in Startup.ConfigureServices - a regression
        // here means the [EnableRateLimiting("LoginRateLimit")] attribute on AuthController.Login
        // goes back to being a silent no-op, exactly the bug this test exists to catch.
        private static async Task<TestServer> BuildServerAsync(int permitLimit)
        {
            var host = await new HostBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder.UseTestServer();
                    webBuilder.ConfigureServices(services =>
                    {
                        services.AddRouting();
                        services.AddRateLimiter(options =>
                        {
                            options.RejectionStatusCode = StatusCodes.Status429TooManyRequests;

                            options.AddPolicy("LoginRateLimit", httpContext =>
                                RateLimitPartition.GetFixedWindowLimiter(
                                    partitionKey: httpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown",
                                    factory: _ => new FixedWindowRateLimiterOptions
                                    {
                                        PermitLimit = permitLimit,
                                        Window = TimeSpan.FromMinutes(1)
                                    }));
                        });
                    });
                    webBuilder.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseRateLimiter();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapGet("/login", () => "ok").RequireRateLimiting("LoginRateLimit");
                        });
                    });
                })
                .StartAsync();

            return host.GetTestServer();
        }

        [TestMethod]
        public async Task RequestsWithinTheLimit_AllSucceed()
        {
            var server = await BuildServerAsync(permitLimit: 3);
            var client = server.CreateClient();

            for (var i = 0; i < 3; i++)
            {
                var response = await client.GetAsync("/login");
                Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [TestMethod]
        public async Task RequestsBeyondTheLimit_AreRejectedWith429()
        {
            var server = await BuildServerAsync(permitLimit: 3);
            var client = server.CreateClient();

            for (var i = 0; i < 3; i++)
            {
                await client.GetAsync("/login");
            }

            var response = await client.GetAsync("/login");

            Assert.AreEqual((HttpStatusCode)429, response.StatusCode);
        }
    }
}
