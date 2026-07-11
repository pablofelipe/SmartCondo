namespace SmartCondoApi.Dto
{
    public class AuthKeyDTO
    {
        public string KeyId { get; set; }

        public string PublicKey { get; set; }

        public DateTime ExpiresAt { get; set; }

        public string Format { get; set; }
    }
}
