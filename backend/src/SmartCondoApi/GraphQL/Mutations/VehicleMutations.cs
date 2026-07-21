using Microsoft.AspNetCore.Http;
using SmartCondoApi.GraphQL.Inputs;
using SmartCondoApi.Infra;
using SmartCondoApi.Models;
using SmartCondoApi.Services.Vehicle;

namespace SmartCondoApi.GraphQL.Mutations
{
    [ExtendObjectType(OperationTypeNames.Mutation)]
    public class VehicleMutations
    {
        public async Task<Vehicle> CreateVehicle(
            [Service] IVehicleService vehicleService,
            [Service] IHttpContextAccessor httpContextAccessor,
            [Service] ILogger<VehicleMutations> logger,
            [Service] IAuthenticatedActorResolver actorResolver,
            VehicleInput input)
        {
            try
            {
                if (!input.UserId.HasValue || input.UserId.Value == 0)
                {
                    throw new GraphQLException("UserID is required");
                }

                var actor = await actorResolver.ResolveAsync(httpContextAccessor.HttpContext!.User);

                var vehicle = new Vehicle
                {
                    Type = input.Type,
                    LicensePlate = input.LicensePlate,
                    Brand = input.Brand,
                    Model = input.Model,
                    Color = input.Color,
                    Enabled = input.Enabled,
                    UserId = input.UserId ?? 0
                };

                var createdVehicle = await vehicleService.CreateVehicleAsync(vehicle, actor);
                return createdVehicle;
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
                // Already a deliberately-shaped client error (missing UserID) - let it through as-is.
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception in {Resolver}", nameof(CreateVehicle));
                throw new GraphQLException(new ErrorBuilder()
                    .SetMessage("An unexpected error occurred while creating the vehicle")
                    .SetCode("VEHICLE_CREATION_ERROR")
                    .Build());
            }
        }

        public async Task<Vehicle> UpdateVehicle(
            [Service] IVehicleService vehicleService,
            [Service] IHttpContextAccessor httpContextAccessor,
            [Service] ILogger<VehicleMutations> logger,
            [Service] IAuthenticatedActorResolver actorResolver,
            [ID] string id,
            VehicleInput input)
        {
            try
            {
                if (!int.TryParse(id, out var idInt))
                {
                    throw new GraphQLException("VehicleID must be numeric");
                }

                if (!input.UserId.HasValue || input.UserId.Value == 0)
                {
                    throw new GraphQLException("UserID is required");
                }

                var actor = await actorResolver.ResolveAsync(httpContextAccessor.HttpContext!.User);

                var vehicle = new Vehicle
                {
                    Id = idInt,
                    Type = input.Type,
                    LicensePlate = input.LicensePlate,
                    Brand = input.Brand,
                    Model = input.Model,
                    Color = input.Color,
                    Enabled = input.Enabled,
                    UserId = input.UserId ?? 0
                };

                var updatedVehicle = await vehicleService.UpdateVehicleAsync(vehicle, actor);

                if (updatedVehicle == null)
                {
                    throw new GraphQLException(new ErrorBuilder()
                        .SetMessage("Vehicle not found")
                        .SetCode("VEHICLE_NOT_FOUND")
                        .SetExtension("id", id)
                        .Build());
                }

                return updatedVehicle;
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
                // Already a deliberately-shaped client error (invalid id, missing UserID, not found) - let it through as-is.
                throw;
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Unhandled exception in {Resolver}", nameof(UpdateVehicle));
                throw new GraphQLException(new ErrorBuilder()
                    .SetMessage("An unexpected error occurred while updating the vehicle")
                    .SetCode("VEHICLE_UPDATE_ERROR")
                    .Build());
            }
        }

        public async Task<bool> DeleteVehicle(
            [Service] IVehicleService vehicleService,
            [Service] IHttpContextAccessor httpContextAccessor,
            [Service] ILogger<VehicleMutations> logger,
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
                var deleted = await vehicleService.DeleteVehicleAsync(idInt, actor);

                if (!deleted)
                {
                    throw new GraphQLException(new ErrorBuilder()
                        .SetMessage("Vehicle not found")
                        .SetCode("VEHICLE_NOT_FOUND")
                        .SetExtension("id", id)
                        .Build());
                }

                return true;
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
                logger.LogError(ex, "Unhandled exception in {Resolver}", nameof(DeleteVehicle));
                throw new GraphQLException(new ErrorBuilder()
                    .SetMessage("An unexpected error occurred while deleting the vehicle")
                    .SetCode("VEHICLE_DELETION_ERROR")
                    .Build());
            }
        }
    }
}
