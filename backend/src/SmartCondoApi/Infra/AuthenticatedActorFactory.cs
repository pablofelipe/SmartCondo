using System.Security.Claims;
using SmartCondoApi.Models.Permissions;

namespace SmartCondoApi.Infra
{
    public static class AuthenticatedActorFactory
    {
        public static AuthenticatedActor FromClaimsPrincipal(ClaimsPrincipal user)
        {
            var id = user.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = user.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(id) || !long.TryParse(id, out var actorId) || string.IsNullOrEmpty(role))
            {
                throw new UnauthorizedAccessException("The authenticated principal does not carry a valid actor identity");
            }

            return new AuthenticatedActor(actorId, role);
        }
    }
}
