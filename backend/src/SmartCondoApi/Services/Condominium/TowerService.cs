using Microsoft.EntityFrameworkCore;
using SmartCondoApi.Dto;
using SmartCondoApi.Exceptions;
using SmartCondoApi.Models;

namespace SmartCondoApi.Services.Condominium
{
    public class TowerService(SmartCondoContext _context) : ITowerService
    {
        public async Task<TowerResponseDTO> Get(int id)
        {
            var tower = await _context.Towers
                .Include(t => t.Condominium)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (tower == null)
            {
                throw new TowerNotFoundException($"Torre com ID {id} não encontrada");
            }

            return MapToDto(tower);
        }

        public async Task<List<TowerResponseDTO>> GetByCondominium(int condominiumId)
        {
            var condominium = await _context.Condominiums.FindAsync(condominiumId);
            if (condominium == null)
            {
                throw new CondominiumNotFoundException($"Condomínio com ID {condominiumId} não encontrado");
            }

            var towers = await _context.Towers
                .Where(t => t.CondominiumId == condominiumId)
                .ToListAsync();

            return towers.Select(MapToDto).ToList();
        }

        public async Task<TowerResponseDTO> Create(TowerCreateDTO towerDto)
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

        public async Task Update(int id, TowerUpdateDTO towerDto)
        {
            var tower = await _context.Towers.FindAsync(id);
            if (tower == null)
            {
                throw new TowerNotFoundException($"Torre com ID {id} não encontrada");
            }

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

        public async Task Delete(int id)
        {
            var tower = await _context.Towers.FindAsync(id);
            if (tower == null)
            {
                throw new TowerNotFoundException($"Torre com ID {id} não encontrada");
            }

            // Verifica se existem usuários associados a esta torre
            var hasUsers = await _context.UserProfiles.AnyAsync(u => u.TowerId == id);
            if (hasUsers)
            {
                throw new InconsistentDataException("Não é possível deletar uma torre com usuários associados");
            }

            _context.Towers.Remove(tower);
            await _context.SaveChangesAsync();
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