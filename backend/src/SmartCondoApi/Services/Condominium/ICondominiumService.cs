using SmartCondoApi.Dto;
using SmartCondoApi.Models.Permissions;

namespace SmartCondoApi.Services.Condominium
{
    public interface ICondominiumService
    {
        Task<IEnumerable<CondominiumResponseDTO>> Get(AuthenticatedActor actor);

        Task<CondominiumResponseDTO> Get(int condominiumId, AuthenticatedActor actor);

        Task<List<UserProfileResponseDTO>> SearchUsers(int condominiumId, UserProfileSearchDTO searchDto, AuthenticatedActor actor);

        Task<List<CondominiumResponseDTO>> Search(CondominiumSearchDTO searchDto, AuthenticatedActor actor);

        Task<CondominiumResponseDTO> Create(CondominiumCreateDTO condoDto, AuthenticatedActor actor);

        Task Update(int id, CondominiumUpdateDTO condoDto, AuthenticatedActor actor);

        Task Delete(int id, AuthenticatedActor actor);
    }
}
