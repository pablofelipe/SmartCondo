using SmartCondoApi.Dto;

namespace SmartCondoApi.Services.Condominium
{
    public interface ICondominiumService
    {
        Task<IEnumerable<CondominiumResponseDTO>> Get();

        Task<CondominiumResponseDTO> Get(int condominiumId);

        Task<List<UserProfileResponseDTO>> SearchUsers(int condominiumId, UserProfileSearchDTO searchDto);

        Task<List<CondominiumResponseDTO>> Search(CondominiumSearchDTO searchDto);

        Task<CondominiumResponseDTO> Create(CondominiumCreateDTO condoDto);

        Task Update(int id, CondominiumUpdateDTO condoDto);

        Task Delete(int id);
    }
}
