
using SmartCondoApi.GraphQL.Inputs;
using SmartCondoApi.Models.Permissions;

namespace SmartCondoApi.Services.Vehicle
{
    public interface IVehicleService
    {
        Task<IEnumerable<Models.Vehicle>> GetFilteredVehiclesAsync(VehicleFilterInput filter, AuthenticatedActor actor);
        Task<Models.Vehicle> GetVehicleByIdAsync(int id, AuthenticatedActor actor);
        Task<Models.Vehicle> CreateVehicleAsync(Models.Vehicle vehicle, AuthenticatedActor actor);
        Task<Models.Vehicle> UpdateVehicleAsync(Models.Vehicle vehicle, AuthenticatedActor actor);
        Task<bool> DeleteVehicleAsync(int id, AuthenticatedActor actor);
    }
}
