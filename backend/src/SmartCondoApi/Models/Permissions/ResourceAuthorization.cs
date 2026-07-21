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
    }
}
