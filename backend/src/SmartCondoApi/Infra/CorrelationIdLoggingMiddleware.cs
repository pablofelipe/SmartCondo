namespace SmartCondoApi.Infra
{
    public class CorrelationIdLoggingMiddleware(RequestDelegate _next, ILogger<CorrelationIdLoggingMiddleware> _logger)
    {
        private const string HeaderName = "X-Correlation-Id";

        public async Task Invoke(HttpContext context)
        {
            var correlationId = context.TraceIdentifier;

            context.Response.Headers[HeaderName] = correlationId;

            using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
            {
                await _next(context);
            }
        }
    }
}
