using System.Security.Claims;
using SmartCondoApi.Models.Permissions;

namespace SmartCondoApi.Infra
{
    public interface IAuthenticatedActorResolver
    {
        Task<AuthenticatedActor> ResolveAsync(ClaimsPrincipal principal);
    }
}
