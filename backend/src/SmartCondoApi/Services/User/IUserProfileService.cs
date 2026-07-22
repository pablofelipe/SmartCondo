using SmartCondoApi.Dto;
using SmartCondoApi.Models.Permissions;

namespace SmartCondoApi.Services.User
{
    public interface IUserProfileService
    {
        Task<UserProfileResponseDTO> Add(UserProfileCreateDTO userCreateDTO, AuthenticatedActor actor);

        Task<UserProfileResponseDTO> Update(long userId, UserProfileUpdateDTO userUpdateDTO, AuthenticatedActor actor);

        Task<UserProfileEditDTO> Get(long id, AuthenticatedActor actor);

        Task Delete(long id, AuthenticatedActor actor);
    }
}
