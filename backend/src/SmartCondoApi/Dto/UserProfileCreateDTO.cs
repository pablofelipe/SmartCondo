namespace SmartCondoApi.Dto
{
    public class UserProfileCreateDTO
    {
        public string Name { get; set; }
        public string Address { get; set; }
        public string Phone1 { get; set; }
        public string Phone2 { get; set; }
        public int UserTypeId { get; set; }
        public string RegistrationNumber { get; set; }
        public int? CondominiumId { get; set; }
        public int? TowerId { get; set; }
        public int? FloorId { get; set; }
        public int? Apartment { get; set; }
        public int? ParkingSpaceNumber { get; set; }
        public UserCreateDTO User { get; set; }
    }
}
