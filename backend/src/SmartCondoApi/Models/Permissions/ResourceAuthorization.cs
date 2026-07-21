using SmartCondoApi.Dto;

namespace SmartCondoApi.Models.Permissions
{
    public static class ResourceAuthorization
    {
        public static bool IsAuthorized(AuthenticatedActor actor, long resourceOwnerId, Func<UserPermissionsDTO, bool> hasCapability)
        {
            if (actor.Id == resourceOwnerId)
            {
                return true;
            }

            return RolePermissions.GetPermissions().TryGetValue(actor.Role, out var permissions) && hasCapability(permissions);
        }

        /// <summary>
        /// The administrative path: Capability AND Scope. Scope never grants authority on its own -
        /// a role with no matching capability stays denied regardless of tenant membership.
        /// </summary>
        public static bool IsAuthorizedInTenant(AuthenticatedActor actor, int? actorTenantId, int? resourceTenantId, Func<UserPermissionsDTO, bool> hasCapability)
        {
            if (!RolePermissions.GetPermissions().TryGetValue(actor.Role, out var permissions) || !hasCapability(permissions))
            {
                return false;
            }

            return permissions.CanManageAllCondominiums || (actorTenantId.HasValue && actorTenantId == resourceTenantId);
        }
    }
}
