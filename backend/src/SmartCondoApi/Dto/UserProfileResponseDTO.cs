
namespace SmartCondoApi.Dto
{
    public class UserProfileResponseDTO
    {
        public long Id { get; set; }
        public string Name { get; set; }
        public string Address { get; set; }
        public int UserTypeId { get; set; }
        public string RegistrationNumber { get; set; }
        public int? CondominiumId { get; set; }
        public int? TowerId { get; set; }
        public int? FloorId { get; set; }
        public int? Apartment { get; set; }
        public int? ParkingSpaceNumber { get; set; }

        public ICollection<VehicleDTO> Vehicles { get; set; } = new List<VehicleDTO>();
        public string Message { get; internal set; }
        public string Token { get; internal set; }
    }
}
