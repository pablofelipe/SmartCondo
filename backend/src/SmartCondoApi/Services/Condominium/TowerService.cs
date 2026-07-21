using Microsoft.EntityFrameworkCore;
using SmartCondoApi.Dto;
using SmartCondoApi.Exceptions;
using SmartCondoApi.Infra;
using SmartCondoApi.Models;
using SmartCondoApi.Models.Permissions;

namespace SmartCondoApi.Services.Condominium
{
    public class TowerService(SmartCondoContext _context) : ITowerService
    {
        public async Task<TowerResponseDTO> Get(int id, AuthenticatedActor actor)
        {
            var tower = await _context.Towers
                .Include(t => t.Condominium)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tower == null)
            {
                throw new TowerNotFoundException($"Torre com ID {id} não encontrada");
            }

            await EnsureAuthorizedAsync(actor, tower.CondominiumId, p => p.CanViewCondominiums, "view");

            return MapToDto(tower);
        }

        public async Task<List<TowerResponseDTO>> GetByCondominium(int condominiumId, AuthenticatedActor actor)
        {
            var condominium = await _context.Condominiums.FindAsync(condominiumId);
            if (condominium == null)
            {
                throw new CondominiumNotFoundException($"Condomínio com ID {condominiumId} não encontrado");
            }

            await EnsureAuthorizedAsync(actor, condominiumId, p => p.CanViewCondominiums, "view towers in");

            var towers = await _context.Towers
                .Where(t => t.CondominiumId == condominiumId)
                .ToListAsync();

            return towers.Select(MapToDto).ToList();
        }

        public async Task<TowerResponseDTO> Create(TowerCreateDTO towerDto, AuthenticatedActor actor)
        {
            if (string.IsNullOrEmpty(towerDto.Name))
            {
                throw new InconsistentDataException("O nome da torre é obrigatório");
            }

            if (towerDto.FloorCount <= 0)
            {
                throw new InconsistentDataException("O número de andares deve ser maior que zero");
            }

            var condominium = await _context.Condominiums.FindAsync(towerDto.CondominiumId);
            if (condominium == null)
            {
                throw new CondominiumNotFoundException($"Condomínio com ID {towerDto.CondominiumId} não encontrado");
            }

            await EnsureAuthorizedAsync(actor, towerDto.CondominiumId, p => p.CanEditCondominiums, "register a tower in");

            var tower = new Tower
            {
                Number = towerDto.Number,
                Name = towerDto.Name,
                CondominiumId = towerDto.CondominiumId,
                FloorCount = towerDto.FloorCount
            };

            await _context.Towers.AddAsync(tower);
            await _context.SaveChangesAsync();

            return MapToDto(tower);
        }

        public async Task Update(int id, TowerUpdateDTO towerDto, AuthenticatedActor actor)
        {
            var tower = await _context.Towers.FindAsync(id);
            if (tower == null)
            {
                throw new TowerNotFoundException($"Torre com ID {id} não encontrada");
            }

            await EnsureAuthorizedAsync(actor, tower.CondominiumId, p => p.CanEditCondominiums, "edit");

            if (towerDto.Number.HasValue)
            {
                tower.Number = towerDto.Number.Value;
            }

            if (!string.IsNullOrEmpty(towerDto.Name))
            {
                tower.Name = towerDto.Name;
            }

            if (towerDto.FloorCount.HasValue)
            {
                if (towerDto.FloorCount <= 0)
                {
                    throw new InconsistentDataException("O número de andares deve ser maior que zero");
                }
                tower.FloorCount = towerDto.FloorCount.Value;
            }

            _context.Towers.Update(tower);
            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id, AuthenticatedActor actor)
        {
            var tower = await _context.Towers.FindAsync(id);
            if (tower == null)
            {
                throw new TowerNotFoundException($"Torre com ID {id} não encontrada");
            }

            await EnsureAuthorizedAsync(actor, tower.CondominiumId, p => p.CanEditCondominiums, "delete");

            // Verifica se existem usuários associados a esta torre
            var hasUsers = await _context.UserProfiles.AnyAsync(u => u.TowerId == id);
            if (hasUsers)
            {
                throw new InconsistentDataException("Não é possível deletar uma torre com usuários associados");
            }

            _context.Towers.Remove(tower);
            await _context.SaveChangesAsync();
        }

        private async Task EnsureAuthorizedAsync(AuthenticatedActor actor, int resourceCondominiumId, Func<UserPermissionsDTO, bool> hasCapability, string action)
        {
            var actorTenantId = await _context.GetActorCondominiumIdAsync(actor.Id);

            if (!ResourceAuthorization.IsAuthorizedInTenant(actor, actorTenantId, resourceCondominiumId, hasCapability))
            {
                throw new UnauthorizedAccessException($"You are not authorized to {action} this tower's condominium");
            }
        }

        private static TowerResponseDTO MapToDto(Tower tower)
        {
            return new TowerResponseDTO
            {
                Id = tower.Id,
                Number = tower.Number,
                Name = tower.Name,
                CondominiumId = tower.CondominiumId,
                FloorCount = tower.FloorCount
            };
        }
    }
}
