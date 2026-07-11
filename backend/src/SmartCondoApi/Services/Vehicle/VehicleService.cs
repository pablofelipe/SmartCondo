using Microsoft.EntityFrameworkCore;
using SmartCondoApi.Models;
using SmartCondoApi.GraphQL.Inputs;
using SmartCondoApi.Exceptions;

namespace SmartCondoApi.Services.Vehicle
{
    public class VehicleService(SmartCondoContext _context) : IVehicleService
    {
        public async Task<Models.Vehicle> CreateVehicleAsync(Models.Vehicle vehicle)
        {
            _context.Vehicles.Add(vehicle);
            await _context.SaveChangesAsync();
            return vehicle;
        }

        public async Task DeleteVehicleAsync(int id)
        {
            var vehicle = await GetVehicleByIdAsync(id);
            if (vehicle != null)
            {
                _context.Vehicles.Remove(vehicle);
                await _context.SaveChangesAsync();
            }
        }

        public async Task<IEnumerable<Models.Vehicle>> GetFilteredVehiclesAsync(VehicleFilterInput filter)
        {
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

        public async Task<Models.Vehicle> GetVehicleByIdAsync(int id)
        {
            return await _context.Vehicles
                .Include(v => v.User)
                .FirstOrDefaultAsync(v => v.Id == id);
        }

        public async Task<Models.Vehicle> UpdateVehicleAsync(Models.Vehicle vehicle)
        {
            _context.Vehicles.Update(vehicle);
            await _context.SaveChangesAsync();
            return vehicle;
        }
    }
}