using Microsoft.EntityFrameworkCore;
using SmartCondoApi.Models;
using SmartCondoApi.Models.Permissions;
using SmartCondoApi.GraphQL.Inputs;
using SmartCondoApi.Exceptions;

namespace SmartCondoApi.Services.Vehicle
{
    public class VehicleService(SmartCondoContext _context) : IVehicleService
    {
        public async Task<Models.Vehicle> CreateVehicleAsync(Models.Vehicle vehicle, AuthenticatedActor actor)
        {
            if (!ResourceAuthorization.IsAuthorized(actor, vehicle.UserId, p => p.CanRegisterVehicles))
            {
                throw new UnauthorizedAccessException("You are not authorized to register this vehicle");
            }

            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();
            return vehicle;
        }

        public async Task<bool> DeleteVehicleAsync(int id, AuthenticatedActor actor)
        {
            var vehicle = await _context.Vehicles.FirstOrDefaultAsync(v => v.Id == id);
            if (vehicle == null)
            {
                return false;
            }

            if (!ResourceAuthorization.IsAuthorized(actor, vehicle.UserId, p => p.CanEditVehicles))
            {
                throw new UnauthorizedAccessException("You are not authorized to delete this vehicle");
            }

            _context.Vehicles.Remove(vehicle);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<IEnumerable<Models.Vehicle>> GetFilteredVehiclesAsync(VehicleFilterInput filter, AuthenticatedActor actor)
        {
            var hasViewCapability = RolePermissions.GetPermissions().TryGetValue(actor.Role, out var permissions) && permissions.CanViewVehicles;

            if (!hasViewCapability)
            {
                return await _context.Vehicles.Where(v => v.UserId == actor.Id).ToListAsync();
            }

            if (string.IsNullOrEmpty(filter.LicensePlate)
                && string.IsNullOrEmpty(filter.Model)
                && string.IsNullOrEmpty(filter.OwnerName)
                && string.IsNullOrEmpty(filter.RegistrationNumber)
                && filter.ApartmentNumber == null
               && filter.ParkingSpaceNumber == null)
                throw new NoVehicleFilterException("Nenhum parâmetro para veículo recebido");

            var query = from vehicle in _context.Vehicles
                        join userProfile in _context.UserProfiles
                        on vehicle.UserId equals userProfile.Id
                        select new Models.Vehicle
                        {
                            Id = vehicle.Id,
                            Type = vehicle.Type,
                            LicensePlate = vehicle.LicensePlate,
                            Brand = vehicle.Brand,
                            Model = vehicle.Model,
                            Color = vehicle.Color,
                            Enabled = vehicle.Enabled,
                            UserId = userProfile.Id,
                            User = userProfile
                        };

            if (!string.IsNullOrEmpty(filter.LicensePlate))
                query = query.Where(v => EF.Functions.ILike(v.LicensePlate, $"%{filter.LicensePlate}%"));

            if (!string.IsNullOrEmpty(filter.Model))
                query = query.Where(v => EF.Functions.ILike(v.Model, $"%{filter.Model}%"));

            if (filter.ApartmentNumber.HasValue)
                query = query.Where(v => v.User.Apartment == filter.ApartmentNumber.Value);

            if (filter.ParkingSpaceNumber.HasValue)
                query = query.Where(v => v.User.ParkingSpaceNumber == filter.ParkingSpaceNumber.Value);

            if (!string.IsNullOrEmpty(filter.OwnerName))
                query = query.Where(v => EF.Functions.ILike(v.User.Name, $"%{filter.OwnerName}%"));

            if (!string.IsNullOrEmpty(filter.RegistrationNumber))
                query = query.Where(v => v.User.RegistrationNumber == filter.RegistrationNumber);

            return await query.ToListAsync();
        }

        public async Task<Models.Vehicle> GetVehicleByIdAsync(int id, AuthenticatedActor actor)
        {
            var vehicle = await _context.Vehicles
                .Include(v => v.User)
                .FirstOrDefaultAsync(v => v.Id == id);

            if (vehicle == null)
            {
                return null;
            }

            if (!ResourceAuthorization.IsAuthorized(actor, vehicle.UserId, p => p.CanViewVehicles))
            {
                throw new UnauthorizedAccessException("You are not authorized to view this vehicle");
            }

            return vehicle;
        }

        public async Task<Models.Vehicle> UpdateVehicleAsync(Models.Vehicle vehicle, AuthenticatedActor actor)
        {
            var existing = await _context.Vehicles.AsNoTracking().FirstOrDefaultAsync(v => v.Id == vehicle.Id);
            if (existing == null)
            {
                return null;
            }

            var isSelf = actor.Id == existing.UserId;
            var hasEditCapability = RolePermissions.GetPermissions().TryGetValue(actor.Role, out var permissions) && permissions.CanEditVehicles;

            if (!isSelf && !hasEditCapability)
            {
                throw new UnauthorizedAccessException("You are not authorized to edit this vehicle");
            }

            if (!hasEditCapability)
            {
                // Self-service can edit its own vehicle, but never reassign it to someone else.
                vehicle.UserId = existing.UserId;
            }

            _context.Vehicles.Update(vehicle);
            await _context.SaveChangesAsync();
            return vehicle;
        }
    }
}