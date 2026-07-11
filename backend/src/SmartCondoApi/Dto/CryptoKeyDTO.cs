using System.Security.Cryptography;

namespace SmartCondoApi.Dto
{
    public class CryptoKeyDTO
    {
        public string KeyId { get; set; }
        public string PublicKeyPem { get; set; }
        public RSAParameters PrivateKey { get; set; }
        public DateTime Expiration { get; set; }
        public bool Expired { get; set; }
    }
}
