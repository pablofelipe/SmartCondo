using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;

namespace SmartCondoApi.Infra
{
    // Shared by both WebSocket connection paths (the Lambda ConnectFunction and the native
    // container endpoint) - a WebSocket handshake can't carry an Authorization header from a
    // browser client, so the JWT travels as a query string parameter and must be validated the
    // same way the ASP.NET Core JWT bearer pipeline validates it for every other request.
    public static class WebSocketTokenValidator
    {
        public static bool TryGetUserId(string? token, IConfiguration configuration, out long userId)
        {
            userId = 0;

            if (string.IsNullOrEmpty(token))
                return false;

            var jwtKey = Environment.GetEnvironmentVariable("JWT_KEY") ?? configuration["Jwt:Key"];

            if (string.IsNullOrEmpty(jwtKey))
                return false;

            var validationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = configuration["Jwt:Issuer"],
                ValidAudience = configuration["Jwt:Audience"],
                IssuerSigningKey = new SymmetricSecurityKey(Convert.FromBase64String(jwtKey))
            };

            try
            {
                var principal = new JwtSecurityTokenHandler().ValidateToken(token, validationParameters, out _);
                var subject = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;

                return !string.IsNullOrEmpty(subject) && long.TryParse(subject, out userId);
            }
            catch (SecurityTokenException)
            {
                return false;
            }
        }
    }
}
