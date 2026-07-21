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
                throw new TowerNotFoundException($"Tower with ID {id} not found");
            }

            EnsureAuthorized(actor, tower.CondominiumId, p => p.CanViewCondominiums, "view");

            return MapToDto(tower);
        }

        public async Task<List<TowerResponseDTO>> GetByCondominium(int condominiumId, AuthenticatedActor actor)
        {
            var condominium = await _context.Condominiums.FindAsync(condominiumId);
            if (condominium == null)
            {
                throw new CondominiumNotFoundException($"Condominium with ID {condominiumId} not found");
            }

            EnsureAuthorized(actor, condominiumId, p => p.CanViewCondominiums, "view towers in");

            var towers = await _context.Towers
                .Where(t => t.CondominiumId == condominiumId)
                .ToListAsync();

            return towers.Select(MapToDto).ToList();
        }

        public async Task<TowerResponseDTO> Create(TowerCreateDTO towerDto, AuthenticatedActor actor)
        {
            if (string.IsNullOrEmpty(towerDto.Name))
            {
                throw new InconsistentDataException("The tower name is required");
            }

            if (towerDto.FloorCount <= 0)
            {
                throw new InconsistentDataException("The floor count must be greater than zero");
            }

            var condominium = await _context.Condominiums.FindAsync(towerDto.CondominiumId);
            if (condominium == null)
            {
                throw new CondominiumNotFoundException($"Condominium with ID {towerDto.CondominiumId} not found");
            }

            EnsureAuthorized(actor, towerDto.CondominiumId, p => p.CanEditCondominiums, "register a tower in");

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
                throw new TowerNotFoundException($"Tower with ID {id} not found");
            }

            EnsureAuthorized(actor, tower.CondominiumId, p => p.CanEditCondominiums, "edit");

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
                    throw new InconsistentDataException("The floor count must be greater than zero");
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
                throw new TowerNotFoundException($"Tower with ID {id} not found");
            }

            EnsureAuthorized(actor, tower.CondominiumId, p => p.CanEditCondominiums, "delete");

            // Check whether any users are associated with this tower
            var hasUsers = await _context.UserProfiles.AnyAsync(u => u.TowerId == id);
            if (hasUsers)
            {
                throw new InconsistentDataException("Cannot delete a tower with associated users");
            }

            _context.Towers.Remove(tower);
            await _context.SaveChangesAsync();
        }

        private void EnsureAuthorized(AuthenticatedActor actor, int resourceCondominiumId, Func<UserPermissionsDTO, bool> hasCapability, string action)
        {
            var actorTenantId = actor.CondominiumId;

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
