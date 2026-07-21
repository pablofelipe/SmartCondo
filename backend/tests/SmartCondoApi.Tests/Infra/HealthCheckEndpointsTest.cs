using System.Net;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.Hosting;

namespace SmartCondoApi.Tests.Infra
{
    [TestClass]
    public class HealthCheckEndpointsTest
    {
        // A fake check the test can flip between healthy and unhealthy, so /health/ready's behavior
        // can be proven without a real database - mirrors the "ready" tag Startup.cs registers.
        private class ToggleableCheck(Func<HealthCheckResult> resultFactory) : IHealthCheck
        {
            public Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
                => Task.FromResult(resultFactory());
        }

        private static async Task<TestServer> BuildServerAsync(Func<HealthCheckResult> readyCheckResult)
        {
            var host = await new HostBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder.UseTestServer();
                    webBuilder.ConfigureServices(services =>
                    {
                        services.AddRouting();
                        services.AddHealthChecks()
                            .AddCheck("database", () => readyCheckResult(), tags: ["ready"]);
                    });
                    webBuilder.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseEndpoints(endpoints =>
                        {
                            endpoints.MapHealthChecks("/health/live", new HealthCheckOptions
                            {
                                Predicate = _ => false
                            });

                            endpoints.MapHealthChecks("/health/ready", new HealthCheckOptions
                            {
                                Predicate = check => check.Tags.Contains("ready")
                            });
                        });
                    });
                })
                .StartAsync();

            return host.GetTestServer();
        }

        [TestMethod]
        public async Task Live_AlwaysReturns200_RegardlessOfDependencyHealth()
        {
            var server = await BuildServerAsync(() => HealthCheckResult.Unhealthy());
            var client = server.CreateClient();

            var response = await client.GetAsync("/health/live");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task Ready_DatabaseHealthy_Returns200()
        {
            var server = await BuildServerAsync(() => HealthCheckResult.Healthy());
            var client = server.CreateClient();

            var response = await client.GetAsync("/health/ready");

            Assert.AreEqual(HttpStatusCode.OK, response.StatusCode);
        }

        [TestMethod]
        public async Task Ready_DatabaseUnhealthy_Returns503()
        {
            var server = await BuildServerAsync(() => HealthCheckResult.Unhealthy("db down"));
            var client = server.CreateClient();

            var response = await client.GetAsync("/health/ready");

            Assert.AreEqual(HttpStatusCode.ServiceUnavailable, response.StatusCode);
        }
    }
}
