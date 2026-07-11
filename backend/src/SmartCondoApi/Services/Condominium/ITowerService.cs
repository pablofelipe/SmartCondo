using SmartCondoApi.Dto;

namespace SmartCondoApi.Services.Condominium
{
    public interface ITowerService
    {
        Task<TowerResponseDTO> Get(int id);
        Task<List<TowerResponseDTO>> GetByCondominium(int condominiumId);
        Task<TowerResponseDTO> Create(TowerCreateDTO towerDto);
        Task Update(int id, TowerUpdateDTO towerDto);
        Task Delete(int id);
    }
}