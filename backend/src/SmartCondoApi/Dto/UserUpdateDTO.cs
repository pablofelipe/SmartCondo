namespace SmartCondoApi.Dto
{
    public class UserUpdateDTO
    {
        public string? Email { get; set; }
        public string? Password { get; set; }
        public DateOnly Expiration { get; set; }
        public bool? Enabled { get; set; }

        public string KeyId { get; set; }
    }
}
