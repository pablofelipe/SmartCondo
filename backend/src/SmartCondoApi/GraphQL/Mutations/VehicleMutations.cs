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
            VehicleInput input)
        {
            try
            {
                if (!input.UserId.HasValue || input.UserId.Value == 0)
                {
                    throw new GraphQLException("UserID é obrigatório");
                }

                var actor = AuthenticatedActorFactory.FromClaimsPrincipal(httpContextAccessor.HttpContext!.User);

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
            catch (Exception ex)
            {
                throw new GraphQLException(new ErrorBuilder()
                    .SetMessage(ex.Message)
                    .SetCode("VEHICLE_CREATION_ERROR")
                    .SetExtension("input", input)
                    .Build());
            }
        }

        public async Task<Vehicle> UpdateVehicle(
            [Service] IVehicleService vehicleService,
            [Service] IHttpContextAccessor httpContextAccessor,
            [ID] string id,
            VehicleInput input)
        {
            try
            {
                if (!int.TryParse(id, out var idInt))
                {
                    throw new GraphQLException("VehicleID deve ser numérico");
                }

                if (!input.UserId.HasValue || input.UserId.Value == 0)
                {
                    throw new GraphQLException("UserID é obrigatório");
                }

                var actor = AuthenticatedActorFactory.FromClaimsPrincipal(httpContextAccessor.HttpContext!.User);

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
                        .SetMessage("Veículo não encontrado")
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
            catch (Exception ex)
            {
                throw new GraphQLException(new ErrorBuilder()
                    .SetMessage(ex.Message)
                    .SetCode("VEHICLE_UPDATE_ERROR")
                    .SetExtension("id", id)
                    .Build());
            }
        }

        public async Task<bool> DeleteVehicle(
            [Service] IVehicleService vehicleService,
            [Service] IHttpContextAccessor httpContextAccessor,
            [ID] string id)
        {
            try
            {
                if (!int.TryParse(id, out var idInt))
                {
                    throw new GraphQLException("VehicleID deve ser numérico");
                }

                var actor = AuthenticatedActorFactory.FromClaimsPrincipal(httpContextAccessor.HttpContext!.User);
                var deleted = await vehicleService.DeleteVehicleAsync(idInt, actor);

                if (!deleted)
                {
                    throw new GraphQLException(new ErrorBuilder()
                        .SetMessage("Veículo não encontrado")
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
            catch (Exception ex)
            {
                throw new GraphQLException(new ErrorBuilder()
                    .SetMessage(ex.Message)
                    .SetCode("VEHICLE_DELETION_ERROR")
                    .SetExtension("id", id)
                    .Build());
            }
        }
    }
}
