namespace SmartCondoApi.Dto
{
    public class UserCreateDTO
    {
        public string Email { get; set; }
        public string Password { get; set; }
        public DateTimeOffset Expiration { get; set; }
        public bool Enabled { get; set; }
        public string KeyId { get; set; }
    }
}
