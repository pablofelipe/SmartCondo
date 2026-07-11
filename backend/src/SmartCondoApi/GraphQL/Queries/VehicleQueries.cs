using SmartCondoApi.GraphQL.Inputs;
using SmartCondoApi.Models;
using SmartCondoApi.Services.Vehicle;

namespace SmartCondoApi.GraphQL.Queries
{
    [ExtendObjectType(OperationTypeNames.Query)]
    public class VehicleQueries
    {
        //[UsePaging]
        //[UseProjection]
        //[UseFiltering]
        //[UseSorting]
        public async Task<IEnumerable<Vehicle>> GetVehicles(
            [Service] IVehicleService vehicleService,
            [GraphQLType(typeof(VehicleFilterInputType))] VehicleFilterInput? filter = null)
        {
            try
            {
                return await vehicleService.GetFilteredVehiclesAsync(filter ?? new VehicleFilterInput());
            }
            catch (Exception ex)
            {
                throw new GraphQLException(new ErrorBuilder()
                    .SetMessage(ex.Message)
                    .SetCode("VEHICLE_FETCH_ERROR")
                    .Build());
            }
        }

        public async Task<Vehicle> GetVehicle(
            [Service] IVehicleService vehicleService,
            [ID] string id)
        {
            try
            {
                if (!int.TryParse(id, out var idInt))
                {
                    throw new GraphQLException("VehicleID deve ser numérico");
                }

                var vehicle = await vehicleService.GetVehicleByIdAsync(idInt);
                return vehicle ?? throw new GraphQLException(new ErrorBuilder()
                    .SetMessage("Veículo não encontrado")
                    .SetCode("VEHICLE_NOT_FOUND")
                    .SetExtension("id", id)
                    .Build());
            }
            catch (Exception ex)
            {
                throw new GraphQLException(new ErrorBuilder()
                    .SetMessage(ex.Message)
                    .SetCode("VEHICLE_FETCH_ERROR")
                    .Build());
            }
        }
    }
}
