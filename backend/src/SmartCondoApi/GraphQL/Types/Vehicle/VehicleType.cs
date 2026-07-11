using Microsoft.EntityFrameworkCore;
using SmartCondoApi.Models;

namespace SmartCondoApi.GraphQL.Types.Vehicle
{
    public class VehicleType : ObjectType<Models.Vehicle>
    {
        protected override void Configure(IObjectTypeDescriptor<Models.Vehicle> descriptor)
        {
            descriptor.Description("Represents a vehicle in the condominium system.");

            descriptor.Field(v => v.Id)
                .Type<NonNullType<IdType>>();

            descriptor
                .Field(v => v.User)
                .Description("The owner of this vehicle.")
                .ResolveWith<Resolvers>(r => r.GetUserAsync(default!, default!, default!));

            descriptor
                .Field(v => v.Type)
                .Description("The type of vehicle (Car, Motorcycle, Truck).");
        }

        private class Resolvers
        {
            public async Task<UserProfile> GetUserAsync(
                [Parent] Models.Vehicle vehicle,
                [Service] SmartCondoContext context,
                CancellationToken cancellationToken)
            {
                return await context.UserProfiles
                    .FirstOrDefaultAsync(u => u.Id == vehicle.UserId, cancellationToken);
            }
        }
    }
}
