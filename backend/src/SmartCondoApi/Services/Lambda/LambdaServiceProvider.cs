using Microsoft.EntityFrameworkCore;
using SmartCondoApi.Functions;
using SmartCondoApi.Models;

namespace SmartCondoApi.Services.Lambda
{
    public static class LambdaServiceProvider
    {
        private static readonly ServiceProvider _serviceProvider;

        static LambdaServiceProvider()
        {
            var services = new ServiceCollection();

            var dbHost = Environment.GetEnvironmentVariable("DB_HOST");
            var dbName = Environment.GetEnvironmentVariable("DB_NAME");
            var dbUser = Environment.GetEnvironmentVariable("DB_USER");
            var dbPassword = Environment.GetEnvironmentVariable("DB_PASSWORD");

            var connectionString = $"Host={dbHost};Database={dbName};Username={dbUser};Password={dbPassword}";

            services.AddDbContext<SmartCondoContext>(options =>
                options.UseNpgsql(connectionString));

            services.AddScoped<WebSocketFunctions>();

            _serviceProvider = services.BuildServiceProvider();
        }

        public static ServiceProvider GetServiceProvider()
        {
            return _serviceProvider;
        }
    }
}
