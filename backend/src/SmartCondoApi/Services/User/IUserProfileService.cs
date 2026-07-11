using SmartCondoApi.Dto;
using SmartCondoApi.Models;

namespace SmartCondoApi.Services.User
{
    public interface IUserProfileService
    {
        Task<UserProfileResponseDTO> Add(UserProfileCreateDTO userCreateDTO);

        Task<UserProfileResponseDTO> Update(long userId, UserProfileUpdateDTO userUpdateDTO);

        Task<IEnumerable<UserProfile>> Get();

        Task<UserProfileEditDTO> Get(long id);

        Task Delete(long id);
    }
}
