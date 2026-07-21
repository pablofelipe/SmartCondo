using Microsoft.EntityFrameworkCore;
using SmartCondoApi.Dto;
using SmartCondoApi.Exceptions;
using SmartCondoApi.Infra;
using SmartCondoApi.Models;
using SmartCondoApi.Models.Permissions;

namespace SmartCondoApi.Services.Condominium
{
    public class CondominiumService(SmartCondoContext _context) : ICondominiumService
    {
        public async Task<IEnumerable<CondominiumResponseDTO>> Get(AuthenticatedActor actor)
        {
            if (!RolePermissions.GetPermissions().TryGetValue(actor.Role, out var permissions) || !permissions.CanViewCondominiums)
            {
                return [];
            }

            IQueryable<Models.Condominium> query = _context.Condominiums.Include(c => c.Towers);

            if (!permissions.CanManageAllCondominiums)
            {
                var actorTenantId = await _context.GetActorCondominiumIdAsync(actor.Id);
                query = query.Where(c => c.Id == actorTenantId);
            }

            var condos = await query.ToListAsync();

            return condos.Select(MapToDto).ToList();
        }

        public async Task<CondominiumResponseDTO> Get(int condominiumId, AuthenticatedActor actor)
        {
            var condo = await _context.Condominiums
                .Include(c => c.Towers)
                .FirstOrDefaultAsync(c => c.Id == condominiumId);

            if (condo == null)
            {
                throw new CondominiumNotFoundException($"Condominium with ID {condominiumId} not found");
            }

            await EnsureAuthorizedAsync(actor, condo.Id, p => p.CanViewCondominiums, "view");

            return MapToDto(condo);
        }

        public async Task<List<UserProfileResponseDTO>> SearchUsers(int condominiumId, UserProfileSearchDTO searchDto, AuthenticatedActor actor)
        {
            if (condominiumId < 1)
            {
                throw new InconsistentDataException($"Invalid condominium number {condominiumId}.");
            }

            if (string.IsNullOrEmpty(searchDto.Name) && string.IsNullOrEmpty(searchDto.RegistrationNumber))
            {
                throw new InconsistentDataException("Name or CPF/CNPJ must be provided");
            }

            await EnsureAuthorizedAsync(actor, condominiumId, p => p.CanViewUsers, "search users in");

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

        public async Task<List<CondominiumResponseDTO>> Search(CondominiumSearchDTO searchDto, AuthenticatedActor actor)
        {
            if (string.IsNullOrEmpty(searchDto.Name))
            {
                throw new InconsistentDataException("The name must be provided to search");
            }

            if (!RolePermissions.GetPermissions().TryGetValue(actor.Role, out var permissions) || !permissions.CanViewCondominiums)
            {
                return [];
            }

            IQueryable<Models.Condominium> query = _context.Condominiums
                .Where(c => EF.Functions.Like(c.Name.ToLower(), $"%{searchDto.Name.ToLower()}%"));

            if (!permissions.CanManageAllCondominiums)
            {
                var actorTenantId = await _context.GetActorCondominiumIdAsync(actor.Id);
                query = query.Where(c => c.Id == actorTenantId);
            }

            var condos = await query.ToListAsync();
            return condos.Select(MapToDto).ToList();
        }

        public async Task<CondominiumResponseDTO> Create(CondominiumCreateDTO condoDto, AuthenticatedActor actor)
        {
            if (!RolePermissions.GetPermissions().TryGetValue(actor.Role, out var permissions) || !permissions.CanRegisterCondominiums)
            {
                throw new UnauthorizedAccessException("You are not authorized to register a condominium");
            }

            if (string.IsNullOrEmpty(condoDto.Name))
            {
                throw new InconsistentDataException("The condominium name is required");
            }

            if (condoDto.TowerCount < 0)
            {
                throw new InconsistentDataException("The tower count cannot be negative");
            }

            if (condoDto.MaxUsers <= 0)
            {
                throw new InconsistentDataException("The maximum number of users must be greater than zero");
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

                // Update the tower count with the actual number of towers
                condo.TowerCount = condoDto.Towers.Count;
                _context.Condominiums.Update(condo);
                await _context.SaveChangesAsync();
            }

            return MapToDto(condo);
        }

        public async Task Update(int id, CondominiumUpdateDTO condoDto, AuthenticatedActor actor)
        {
            var condo = await _context.Condominiums.FindAsync(id);
            if (condo == null)
            {
                throw new CondominiumNotFoundException($"Condominium with ID {id} not found");
            }

            await EnsureAuthorizedAsync(actor, condo.Id, p => p.CanEditCondominiums, "edit");

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
                    throw new InconsistentDataException("The tower count cannot be negative");
                }
                condo.TowerCount = condoDto.TowerCount.Value;
            }

            if (condoDto.MaxUsers.HasValue)
            {
                if (condoDto.MaxUsers <= 0)
                {
                    throw new InconsistentDataException("The maximum number of users must be greater than zero");
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
                // Remove towers that are no longer in the DTO
                var towersToRemove = condo.Towers
                    .Where(t => !condoDto.Towers.Any(dto => dto.Id == t.Id))
                    .ToList();

                _context.Towers.RemoveRange(towersToRemove);

                // Update existing towers or add new ones
                foreach (var towerDto in condoDto.Towers)
                {
                    if (towerDto.Id.HasValue)
                    {
                        // Update the existing tower
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
                        // Add a new tower
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

        public async Task Delete(int id, AuthenticatedActor actor)
        {
            var condo = await _context.Condominiums.FindAsync(id);
            if (condo == null)
            {
                throw new CondominiumNotFoundException($"Condominium with ID {id} not found");
            }

            await EnsureAuthorizedAsync(actor, condo.Id, p => p.CanEditCondominiums, "delete");

            // Check whether any users are associated with this condominium
            var hasUsers = await _context.UserProfiles.AnyAsync(u => u.CondominiumId == id);
            if (hasUsers)
            {
                // Disable instead of deleting if there are associated users
                condo.Enabled = false;
                _context.Condominiums.Update(condo);
            }
            else
            {
                _context.Condominiums.Remove(condo);
            }

            await _context.SaveChangesAsync();
        }

        private async Task EnsureAuthorizedAsync(AuthenticatedActor actor, int resourceCondominiumId, Func<UserPermissionsDTO, bool> hasCapability, string action)
        {
            var actorTenantId = await _context.GetActorCondominiumIdAsync(actor.Id);

            if (!ResourceAuthorization.IsAuthorizedInTenant(actor, actorTenantId, resourceCondominiumId, hasCapability))
            {
                throw new UnauthorizedAccessException($"You are not authorized to {action} this condominium");
            }
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
