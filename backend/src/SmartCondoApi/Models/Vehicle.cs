namespace SmartCondoApi.Models
{
    public enum VehicleTypeEnum
    {
        Car,
        Motorcycle,
        Truck
    }

    public class Vehicle
    {
        public int Id { get; set; } // PK

        public VehicleTypeEnum Type { get; set; }
        public string LicensePlate { get; set; }
        public string Brand { get; set; }
        public string Model { get; set; }
        public string Color { get; set; }
        public bool Enabled { get; set; }

        // Relacionamento com User (dono do veiculo)
        public long UserId { get; set; } // FK
        public UserProfile User { get; set; }

    }
}
