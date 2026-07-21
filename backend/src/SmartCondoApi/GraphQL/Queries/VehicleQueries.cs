using Microsoft.AspNetCore.Http;
using SmartCondoApi.GraphQL.Inputs;
using SmartCondoApi.Infra;
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
            [Service] IHttpContextAccessor httpContextAccessor,
            [Service] ILogger<VehicleQueries> logger,
            [Service] IAuthenticatedActorResolver actorResolver,
            [GraphQLType(typeof(VehicleFilterInputType))] VehicleFilterInput? filter = null)
        {
            try
            {
                var actor = await actorResolver.ResolveAsync(httpContextAccessor.HttpContext!.User);
                return await vehicleService.GetFilteredVehiclesAsync(filter ?? new VehicleFilterInput(), actor);
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new GraphQLException(new ErrorBuilder()
                    .SetMessage(ex.Message)
                    .SetCode("FORBIDDEN")
                    .Build());
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception in {Resolver}", nameof(GetVehicles));
                throw new GraphQLException(new ErrorBuilder()
                    .SetMessage("An unexpected error occurred while fetching vehicles")
                    .SetCode("VEHICLE_FETCH_ERROR")
                    .Build());
            }
        }

        public async Task<Vehicle> GetVehicle(
            [Service] IVehicleService vehicleService,
            [Service] IHttpContextAccessor httpContextAccessor,
            [Service] ILogger<VehicleQueries> logger,
            [Service] IAuthenticatedActorResolver actorResolver,
            [ID] string id)
        {
            try
            {
                if (!int.TryParse(id, out var idInt))
                {
                    throw new GraphQLException("VehicleID must be numeric");
                }

                var actor = await actorResolver.ResolveAsync(httpContextAccessor.HttpContext!.User);
                var vehicle = await vehicleService.GetVehicleByIdAsync(idInt, actor);
                return vehicle ?? throw new GraphQLException(new ErrorBuilder()
                    .SetMessage("Vehicle not found")
                    .SetCode("VEHICLE_NOT_FOUND")
                    .SetExtension("id", id)
                    .Build());
            }
            catch (UnauthorizedAccessException ex)
            {
                throw new GraphQLException(new ErrorBuilder()
                    .SetMessage(ex.Message)
                    .SetCode("FORBIDDEN")
                    .Build());
            }
            catch (GraphQLException)
            {
                // Already a deliberately-shaped client error (invalid id, not found) - let it through as-is.
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception in {Resolver}", nameof(GetVehicle));
                throw new GraphQLException(new ErrorBuilder()
                    .SetMessage("An unexpected error occurred while fetching the vehicle")
                    .SetCode("VEHICLE_FETCH_ERROR")
                    .Build());
            }
        }
    }
}
