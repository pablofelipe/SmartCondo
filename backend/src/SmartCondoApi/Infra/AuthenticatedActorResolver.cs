using System.Security.Claims;
using Microsoft.EntityFrameworkCore;
using SmartCondoApi.Models;
using SmartCondoApi.Models.Permissions;

namespace SmartCondoApi.Infra
{
    public class AuthenticatedActorResolver(SmartCondoContext _context) : IAuthenticatedActorResolver
    {
        public async Task<AuthenticatedActor> ResolveAsync(ClaimsPrincipal principal)
        {
            var id = principal.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            var role = principal.FindFirst(ClaimTypes.Role)?.Value;

            if (string.IsNullOrEmpty(id) || !long.TryParse(id, out var actorId) || string.IsNullOrEmpty(role))
            {
                throw new UnauthorizedAccessException("The authenticated principal does not carry a valid actor identity");
            }

            var userProfile = await _context.UserProfiles
                .Include(u => u.User)
                .Include(u => u.Condominium)
                .FirstOrDefaultAsync(u => u.Id == actorId);

            if (userProfile?.User == null)
            {
                throw new UnauthorizedAccessException("The authenticated principal does not carry a valid actor identity");
            }

            if (!userProfile.User.Enabled)
            {
                throw new UnauthorizedAccessException("This account has been disabled");
            }

            var condominiumEnabled = userProfile.Condominium?.Enabled ?? true;

            if (userProfile.CondominiumId.HasValue && !condominiumEnabled)
            {
                throw new UnauthorizedAccessException("This account's condominium has been disabled");
            }

            return new AuthenticatedActor(actorId, role, userProfile.User.Enabled, userProfile.CondominiumId, condominiumEnabled);
        }
    }
}
