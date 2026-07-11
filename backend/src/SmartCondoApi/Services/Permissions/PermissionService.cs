using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using SmartCondoApi.Dto;

namespace SmartCondoApi.Services.Permissions
{
    public class PermissionService(RoleManager<IdentityRole<long>> roleManager, IMemoryCache cache) : IPermissionService
    {
        private readonly RoleManager<IdentityRole<long>> _roleManager = roleManager;
        private readonly IMemoryCache _cache = cache;
        private const string PermissionsCacheKey = "UserTypePermissions";

        public async Task<Dictionary<string, UserPermissionsDTO>> GetPermissionsAsync()
        {
            return await _cache.GetOrCreateAsync(PermissionsCacheKey, async entry =>
            {
                entry.AbsoluteExpirationRelativeToNow = TimeSpan.FromHours(1); // Cache por 1 hora
                return await LoadPermissionsFromDbAsync();
            });
        }

        private async Task<Dictionary<string, UserPermissionsDTO>> LoadPermissionsFromDbAsync()
        {
            var permissions = new Dictionary<string, UserPermissionsDTO>();

            // Carrega todas as roles do sistema
            var roles = await _roleManager.Roles.ToListAsync();

            foreach (var role in roles)
            {
                // Obtém todas as claims da role
                var claims = await _roleManager.GetClaimsAsync(role);

                var permission = new UserPermissionsDTO
                {
                    CanSendToIndividuals = claims.Any(c => c.Type == "Permission" && c.Value == "CanSendToIndividuals"),
                    CanSendToGroups = claims.Any(c => c.Type == "Permission" && c.Value == "CanSendToGroups"),
                    CanReceiveMessages = claims.Any(c => c.Type == "Permission" && c.Value == "CanReceiveMessages"),
                    AllowedRecipientTypes = [.. claims
                        .Where(c => c.Type == "AllowedRecipient")
                        .Select(c => c.Value)]
                };

                permissions.Add(role.Name, permission);
            }

            return permissions;
        }
    }
}
