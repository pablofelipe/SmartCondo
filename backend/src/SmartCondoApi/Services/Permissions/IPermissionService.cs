using SmartCondoApi.Dto;

namespace SmartCondoApi.Services.Permissions
{
    public interface IPermissionService
    {
        Task<Dictionary<string, UserPermissionsDTO>> GetPermissionsAsync();
    }
}
