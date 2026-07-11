
using SmartCondoApi.GraphQL.Inputs;

namespace SmartCondoApi.Services.Vehicle
{
    public interface IVehicleService
    {
        Task<IEnumerable<Models.Vehicle>> GetFilteredVehiclesAsync(VehicleFilterInput filter);
        Task<Models.Vehicle> GetVehicleByIdAsync(int id);
        Task<Models.Vehicle> CreateVehicleAsync(Models.Vehicle vehicle);
        Task<Models.Vehicle> UpdateVehicleAsync(Models.Vehicle vehicle);
        Task DeleteVehicleAsync(int id);
    }
}
