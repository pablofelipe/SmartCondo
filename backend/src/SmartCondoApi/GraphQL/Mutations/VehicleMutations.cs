using SmartCondoApi.GraphQL.Inputs;
using SmartCondoApi.Models;
using SmartCondoApi.Services.Vehicle;

namespace SmartCondoApi.GraphQL.Mutations
{
    [ExtendObjectType(OperationTypeNames.Mutation)]
    public class VehicleMutations
    {
        public async Task<Vehicle> CreateVehicle(
            [Service] IVehicleService vehicleService,
            VehicleInput input)
        {
            try
            {
                if (!input.UserId.HasValue || input.UserId.Value == 0)
                {
                    throw new GraphQLException("UserID é obrigatório");
                }

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

                var createdVehicle = await vehicleService.CreateVehicleAsync(vehicle);
                return createdVehicle;
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

                var vehicle = await vehicleService.GetVehicleByIdAsync(idInt);
                if (vehicle == null)
                {
                    throw new GraphQLException(new ErrorBuilder()
                        .SetMessage("Veículo não encontrado")
                        .SetCode("VEHICLE_NOT_FOUND")
                        .SetExtension("id", id)
                        .Build());
                }

                vehicle.Type = input.Type;
                vehicle.LicensePlate = input.LicensePlate;
                vehicle.Brand = input.Brand;
                vehicle.Model = input.Model;
                vehicle.Color = input.Color;
                vehicle.Enabled = input.Enabled;
                vehicle.UserId = input.UserId ?? 0;

                var updatedVehicle = await vehicleService.UpdateVehicleAsync(vehicle);
                return updatedVehicle;
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
            [ID] string id)
        {
            try
            {
                if (!int.TryParse(id, out var idInt))
                {
                    throw new GraphQLException("VehicleID deve ser numérico");
                }

                var vehicle = await vehicleService.GetVehicleByIdAsync(idInt);
                if (vehicle == null)
                {
                    throw new GraphQLException(new ErrorBuilder()
                        .SetMessage("Veículo não encontrado")
                        .SetCode("VEHICLE_NOT_FOUND")
                        .SetExtension("id", id)
                        .Build());
                }

                await vehicleService.DeleteVehicleAsync(idInt);
                return true;
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
