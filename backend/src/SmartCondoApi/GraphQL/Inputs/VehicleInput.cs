using SmartCondoApi.Models;

namespace SmartCondoApi.GraphQL.Inputs
{
    public class VehicleInput
    {
        public VehicleTypeEnum Type { get; set; }
        public string LicensePlate { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public string Color { get; set; }
        public bool Enabled { get; set; }
        public long? UserId { get; set; }
    }
}
