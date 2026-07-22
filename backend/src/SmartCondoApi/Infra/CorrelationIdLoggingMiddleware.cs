namespace SmartCondoApi.Infra
{
    public class CorrelationIdLoggingMiddleware(RequestDelegate _next, ILogger<CorrelationIdLoggingMiddleware> _logger)
    {
        private const string HeaderName = "X-Correlation-Id";
        private const int MaxLength = 128;

        public async Task Invoke(HttpContext context)
        {
            var correlationId = ResolveCorrelationId(context);

            context.Response.Headers[HeaderName] = correlationId;

            using (_logger.BeginScope(new Dictionary<string, object> { ["CorrelationId"] = correlationId }))
            {
                await _next(context);
            }
        }

        private static string ResolveCorrelationId(HttpContext context)
        {
            if (context.Request.Headers.TryGetValue(HeaderName, out var values))
            {
                var inbound = values.ToString();
                if (IsValid(inbound))
                {
                    return inbound;
                }
            }

            return context.TraceIdentifier;
        }

        private static bool IsValid(string value)
        {
            if (string.IsNullOrWhiteSpace(value) || value.Length > MaxLength)
            {
                return false;
            }

            foreach (var c in value)
            {
                if (char.IsControl(c))
                {
                    return false;
                }
            }

            return true;
        }
    }
}
