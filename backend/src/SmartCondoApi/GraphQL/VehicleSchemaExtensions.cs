using HotChocolate.Execution.Configuration;
using SmartCondoApi.GraphQL.Inputs;
using SmartCondoApi.GraphQL.Types.Vehicle;
using SmartCondoApi.Models;

namespace SmartCondoApi.GraphQL
{
    public static class VehicleSchemaExtensions
    {
        public static IRequestExecutorBuilder AddVehicleTypes(this IRequestExecutorBuilder builder)
        {
            return builder
                .AddType<VehicleType>()
                .AddType<VehicleInputType>()
                .AddType<VehicleFilterInputType>()
                .AddType<VehicleTypeEnum>();
        }
    }
}