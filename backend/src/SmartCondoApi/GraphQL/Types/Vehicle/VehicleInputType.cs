using SmartCondoApi.GraphQL.Inputs;
using SmartCondoApi.Models;

namespace SmartCondoApi.GraphQL.Types.Vehicle
{
    public class VehicleInputType : InputObjectType<VehicleInput>
    {
        protected override void Configure(IInputObjectTypeDescriptor<VehicleInput> descriptor)
        {
            descriptor.Field(v => v.Type).Type<NonNullType<EnumType<VehicleTypeEnum>>>();
            descriptor.Field(v => v.LicensePlate).Type<NonNullType<StringType>>();
            descriptor.Field(v => v.Brand).Type<NonNullType<StringType>>();
            descriptor.Field(v => v.Model).Type<NonNullType<StringType>>();
            descriptor.Field(v => v.Color).Type<NonNullType<StringType>>();
            descriptor.Field(v => v.Enabled).Type<NonNullType<BooleanType>>();
            descriptor.Field(v => v.UserId).Type<IdType>();
        }
    }
}
