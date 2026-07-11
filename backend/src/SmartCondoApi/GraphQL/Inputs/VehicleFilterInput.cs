
namespace SmartCondoApi.GraphQL.Inputs
{
    public class VehicleFilterInputType : InputObjectType<VehicleFilterInput>
    {
        protected override void Configure(IInputObjectTypeDescriptor<VehicleFilterInput> descriptor)
        {
            descriptor.Name("VehicleFilterInput");

            descriptor.Field(f => f.LicensePlate).Description("Filtro por placa do veículo");
            descriptor.Field(f => f.Model).Description("Filtro por modelo do veículo");
            descriptor.Field(f => f.ApartmentNumber).Description("Filtro por apartamento");
            descriptor.Field(f => f.ParkingSpaceNumber).Description("Filtro por numero de vaga");
            descriptor.Field(f => f.OwnerName).Description("Filtro por dono");
            descriptor.Field(f => f.RegistrationNumber).Description("Filtro por numero de CPF/CNPJ");
        }
    }

    public record VehicleFilterInput(
        string? LicensePlate = null,
        string? Model = null,
        int? ApartmentNumber = null,
        int? ParkingSpaceNumber = null,
        string? OwnerName = null,
        string? RegistrationNumber = null);
}