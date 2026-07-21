using SmartCondoApi.Dto;
using SmartCondoApi.Models.Permissions;

namespace SmartCondoApi.Services.Condominium
{
    public interface ITowerService
    {
        Task<TowerResponseDTO> Get(int id, AuthenticatedActor actor);
        Task<List<TowerResponseDTO>> GetByCondominium(int condominiumId, AuthenticatedActor actor);
        Task<TowerResponseDTO> Create(TowerCreateDTO towerDto, AuthenticatedActor actor);
        Task Update(int id, TowerUpdateDTO towerDto, AuthenticatedActor actor);
        Task Delete(int id, AuthenticatedActor actor);
    }
}