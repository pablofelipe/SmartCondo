using Microsoft.EntityFrameworkCore;
using SmartCondoApi.Dto;
using SmartCondoApi.Exceptions;
using SmartCondoApi.Models;

namespace SmartCondoApi.Services.Condominium
{
    public class CondominiumService(SmartCondoContext _context) : ICondominiumService
    {
        public async Task<IEnumerable<CondominiumResponseDTO>> Get()
        {
            var condos = await _context.Condominiums
                .Include(c => c.Towers)
                .ToListAsync();

            return condos.Select(MapToDto).ToList();
        }

        public async Task<CondominiumResponseDTO> Get(int condominiumId)
        {
            var condo = await _context.Condominiums
                .Include(c => c.Towers)
                .FirstOrDefaultAsync(c => c.Id == condominiumId);

            if (condo == null)
            {
                throw new CondominiumNotFoundException($"Condomínio com ID {condominiumId} não encontrado");
            }

            return MapToDto(condo);
        }

        public async Task<List<UserProfileResponseDTO>> SearchUsers(int condominiumId, UserProfileSearchDTO searchDto)
        {
            if (condominiumId < 1)
            {
                throw new InconsistentDataException($"Numero do condominio {condominiumId} incorreto.");
            }

            if (string.IsNullOrEmpty(searchDto.Name) && string.IsNullOrEmpty(searchDto.RegistrationNumber))
            {
                throw new InconsistentDataException("Nome ou CPF/CNPJ devem ser informados");
            }

            var query = _context.UserProfiles.Where(u => 
                        u.CondominiumId == condominiumId 
                        && null != u.User 
                        && u.User.Enabled == true
                        && u.UserTypeId == searchDto.Type || searchDto.Type == 0);

            if (!string.IsNullOrEmpty(searchDto.Name))
            {
                query = query.Where(u => EF.Functions.Like(u.Name.ToLower(), $"%{searchDto.Name.ToLower()}%"));
            }

            if (!string.IsNullOrEmpty(searchDto.RegistrationNumber))
            {
                query = query.Where(u => u.RegistrationNumber == searchDto.RegistrationNumber);
            }

            var users = await query.ToListAsync();

            var usersDto = users.Select(u => new UserProfileResponseDTO
            {
                Id = u.Id,
                Name = u.Name,
                RegistrationNumber = u.RegistrationNumber,
                UserTypeId = u.UserTypeId,
                CondominiumId = condominiumId,
                FloorId = u.FloorNumber,
                Apartment = u.Apartment,
                ParkingSpaceNumber = u.ParkingSpaceNumber,
            }).ToList();

            return usersDto;
        }

        public async Task<List<CondominiumResponseDTO>> Search(CondominiumSearchDTO searchDto)
        {
            if (string.IsNullOrEmpty(searchDto.Name))
            {
                throw new InconsistentDataException("O nome deve ser informado para busca");
            }

            var query = _context.Condominiums.AsQueryable();

            if (!string.IsNullOrEmpty(searchDto.Name))
            {
                query = query.Where(c => EF.Functions.Like(c.Name.ToLower(), $"%{searchDto.Name.ToLower()}%"));
            }

            var condos = await query.ToListAsync();
            return condos.Select(MapToDto).ToList();
        }

        public async Task<CondominiumResponseDTO> Create(CondominiumCreateDTO condoDto)
        {
            if (string.IsNullOrEmpty(condoDto.Name))
            {
                throw new InconsistentDataException("O nome do condomínio é obrigatório");
            }

            if (condoDto.TowerCount < 0)
            {
                throw new InconsistentDataException("O número de torres não pode ser negativo");
            }

            if (condoDto.MaxUsers <= 0)
            {
                throw new InconsistentDataException("O número máximo de usuários deve ser maior que zero");
            }

            var condo = new Models.Condominium
            {
                Name = condoDto.Name,
                Address = condoDto.Address,
                TowerCount = condoDto.TowerCount,
                MaxUsers = condoDto.MaxUsers,
                Enabled = condoDto.Enabled
            };

            await _context.Condominiums.AddAsync(condo);
            await _context.SaveChangesAsync();

            if (condoDto.Towers != null && condoDto.Towers.Any())
            {
                foreach (var towerDto in condoDto.Towers)
                {
                    var tower = new Tower
                    {
                        Number = towerDto.Number.HasValue ? towerDto.Number.Value : 0,
                        Name = towerDto.Name,
                        FloorCount = towerDto.FloorCount.HasValue ? towerDto.FloorCount.Value : 0,
                        CondominiumId = condo.Id
                    };
                    await _context.Towers.AddAsync(tower);
                }
                await _context.SaveChangesAsync();

                // Atualiza o tower count com o número real de torres
                condo.TowerCount = condoDto.Towers.Count;
                _context.Condominiums.Update(condo);
                await _context.SaveChangesAsync();
            }

            return MapToDto(condo);
        }

        public async Task Update(int id, CondominiumUpdateDTO condoDto)
        {
            var condo = await _context.Condominiums.FindAsync(id);
            if (condo == null)
            {
                throw new CondominiumNotFoundException($"Condomínio com ID {id} não encontrado");
            }

            if (!string.IsNullOrEmpty(condoDto.Name))
            {
                condo.Name = condoDto.Name;
            }

            if (!string.IsNullOrEmpty(condoDto.Address))
            {
                condo.Address = condoDto.Address;
            }

            if (condoDto.TowerCount.HasValue)
            {
                if (condoDto.TowerCount < 0)
                {
                    throw new InconsistentDataException("O número de torres não pode ser negativo");
                }
                condo.TowerCount = condoDto.TowerCount.Value;
            }

            if (condoDto.MaxUsers.HasValue)
            {
                if (condoDto.MaxUsers <= 0)
                {
                    throw new InconsistentDataException("O número máximo de usuários deve ser maior que zero");
                }
                condo.MaxUsers = condoDto.MaxUsers.Value;
            }

            if (condoDto.Enabled.HasValue)
            {
                condo.Enabled = condoDto.Enabled.Value;
            }

            _context.Condominiums.Update(condo);

            if (condoDto.Towers != null)
            {
                // Remove torres que não estão mais no DTO
                var towersToRemove = condo.Towers
                    .Where(t => !condoDto.Towers.Any(dto => dto.Id == t.Id))
                    .ToList();

                _context.Towers.RemoveRange(towersToRemove);

                // Atualiza ou adiciona novas torres
                foreach (var towerDto in condoDto.Towers)
                {
                    if (towerDto.Id.HasValue)
                    {
                        // Atualiza torre existente
                        var existingTower = condo.Towers.FirstOrDefault(t => t.Id == towerDto.Id);
                        if (existingTower != null)
                        {
                            existingTower.Number = towerDto.Number ?? existingTower.Number;
                            existingTower.Name = towerDto.Name ?? existingTower.Name;
                            existingTower.FloorCount = towerDto.FloorCount ?? existingTower.FloorCount;
                        }
                    }
                    else
                    {
                        // Adiciona nova torre
                        var newTower = new Tower
                        {
                            Number = towerDto.Number ?? 0,
                            Name = towerDto.Name ?? string.Empty,
                            FloorCount = towerDto.FloorCount ?? 0,
                            CondominiumId = id
                        };
                        await _context.Towers.AddAsync(newTower);
                    }
                }

            }
            
            await _context.SaveChangesAsync();
        }

        public async Task Delete(int id)
        {
            var condo = await _context.Condominiums.FindAsync(id);
            if (condo == null)
            {
                throw new CondominiumNotFoundException($"Condomínio com ID {id} não encontrado");
            }

            // Verifica se existem usuários associados a este condomínio
            var hasUsers = await _context.UserProfiles.AnyAsync(u => u.CondominiumId == id);
            if (hasUsers)
            {
                // Desativa em vez de deletar se houver usuários associados
                condo.Enabled = false;
                _context.Condominiums.Update(condo);
            }
            else
            {
                _context.Condominiums.Remove(condo);
            }

            await _context.SaveChangesAsync();
        }

        private static CondominiumResponseDTO MapToDto(Models.Condominium condo)
        {
            return new CondominiumResponseDTO
            {
                Id = condo.Id,
                Name = condo.Name,
                Address = condo.Address,
                TowerCount = condo.TowerCount,
                MaxUsers = condo.MaxUsers,
                Enabled = condo.Enabled,
                Towers = condo.Towers?.Select(t => new TowerResponseDTO
                {
                    Id = t.Id,
                    Number = t.Number,
                    Name = t.Name,
                    CondominiumId = t.CondominiumId,
                    FloorCount = t.FloorCount
                }).ToList() ?? new List<TowerResponseDTO>()
            };
        }
    }
}
