using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using SmartCondoApi.Models;

namespace SmartCondoApi.Infra
{
    public class DatabaseHealthCheck(SmartCondoContext _context) : IHealthCheck
    {
        public async Task<HealthCheckResult> CheckHealthAsync(HealthCheckContext context, CancellationToken cancellationToken = default)
        {
            try
            {
                var canConnect = await _context.Database.CanConnectAsync(cancellationToken);

                return canConnect
                    ? HealthCheckResult.Healthy("Database connection is healthy")
                    : HealthCheckResult.Unhealthy("Cannot connect to the database");
            }
            catch (Exception ex)
            {
                return HealthCheckResult.Unhealthy("Database health check threw an exception", ex);
            }
        }
    }
}
