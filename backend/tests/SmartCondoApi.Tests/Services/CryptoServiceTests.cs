using SmartCondoApi.Dto;
using SmartCondoApi.Services.Crypto;
using System.Security.Cryptography;

namespace SmartCondoApi.Tests.Services
{
    public class CryptoServiceTests : ICryptoService
    {

        public const string TestPrivateKeyXml = "<RSAKeyValue><Modulus>21wEnTU+mcD2w0Lfo1Gv4rtcSWsQJQTNa6gio05AOkV/Er9w3Y2D20dz74mONF7kA8J5jJ6FGQbc1Qk9X3fpcP7Z81AU/GjByplT5jbqUW9PZfvTq3s0ZNg6eYFGE6J1RzmWFETZ5qzeB1EeytqIIuQ5GR/qJmGL1wVwOH7lcUwY0wLbv6W1eLBeB2YvVhYd9K7R6B5r7N1lflt1BdF4W1eZ1xMOZ4mUXe5VyKZPE7hQkIRfxtjXf3NyWZz3F7ZOKAq8Gf+I2RG8nQnHe6O6XvKx7EsMLNv+JQWWTEqmGk+8VIWjywL3WnqK0KlWYAR6sZ6kMuSEWMPk=</Modulus><Exponent>AQAB</Exponent><P>3a1Q5NBPOKU8sD+5XkUZi7sKZ7UZvF6KXpRdyz0JYm7PqkZ3YVOmV8VB5dJj5YQ3R6yX7J7ZQX9wD1j1kZ1jQ==</P><Q>6VBAD1XxW1Q9Xj5w8vWQZ7L5+1QZ3J7eQ6Xq9J7KZ1jQ9Xj5w8vWQZ7L5+1QZ3J7eQ6Xq9J7KZ1jQ==</Q><DP>21wEnTU+mcD2w0Lfo1Gv4rtcSWsQJQTNa6gio05AOkV/Er9w3Y2D20dz74mONF7kA8J5jJ6FGQbc1Qk9X3fpcP7Z81AU/GjByplT5jbqUW9PZfvTq3s0ZNg6eYFGE6J1RzmWFETZ5qzeB1EeytqIIuQ5GR/qJmGL1wVwOH7lcUwY0wLbv6W1eLBeB2YvVhYd9K7R6B5r7N1lflt1BdF4W1eZ1xMOZ4mUXe5VyKZPE7hQkIRfxtjXf3NyWZz3F7ZOKAq8Gf+I2RG8nQnHe6O6XvKx7EsMLNv+JQWWTEqmGk+8VIWjywL3WnqK0KlWYAR6sZ6kMuSEWMPk=</DP><DQ>3a1Q5NBPOKU8sD+5XkUZi7sKZ7UZvF6KXpRdyz0JYm7PqkZ3YVOmV8VB5dJj5YQ3R6yX7J7ZQX9wD1j1kZ1jQ==</DQ><InverseQ>6VBAD1XxW1Q9Xj5w8vWQZ7L5+1QZ3J7eQ6Xq9J7KZ1jQ9Xj5w8vWQZ7L5+1QZ3J7eQ6Xq9J7KZ1jQ==</InverseQ><D>21wEnTU+mcD2w0Lfo1Gv4rtcSWsQJQTNa6gio05AOkV/Er9w3Y2D20dz74mONF7kA8J5jJ6FGQbc1Qk9X3fpcP7Z81AU/GjByplT5jbqUW9PZfvTq3s0ZNg6eYFGE6J1RzmWFETZ5qzeB1EeytqIIuQ5GR/qJmGL1wVwOH7lcUwY0wLbv6W1eLBeB2YvVhYd9K7R6B5r7N1lflt1BdF4W1eZ1xMOZ4mUXe5VyKZPE7hQkIRfxtjXf3NyWZz3F7ZOKAq8Gf+I2RG8nQnHe6O6XvKx7EsMLNv+JQWWTEqmGk+8VIWjywL3WnqK0KlWYAR6sZ6kMuSEWMPk=</D></RSAKeyValue>";

        public string DecryptData(string keyId, string encryptedDataBase64)
        {
            return encryptedDataBase64;
        }

        public string EncryptData(string plainText, string publicKeyPem)
        {
            return plainText;
        }

        public static RSAParameters GetTestPrivateKey()
        {
            string xml = TestPrivateKeyXml.Trim().Replace("\n", "").Replace("\r", "");

            using var rsa = RSA.Create();
            rsa.FromXmlString(xml);
            return rsa.ExportParameters(true);
        }


        public static string GetTestPublicKeyPem()
        {
            return @"-----BEGIN PUBLIC KEY-----
MIIBIjANBgkqhkiG9w0BAQEFAAOCAQ8AMIIBCgKCAQEA21wEnTU+mcD2w0Lfo1Gv
4rtcSWsQJQTNa6gio05AOkV/Er9w3Y2D20dz74mONF7kA8J5jJ6FGQbc1Qk9X3fp
cP7Z81AU/GjByplT5jbqUW9PZfvTq3s0ZNg6eYFGE6J1RzmWFETZ5qzeB1EeytqI
IuQ5GR/qJmGL1wVwOH7lcUwY0wLbv6W1eLBeB2YvVhYd9K7R6B5r7N1lflt1BdF
4W1eZ1xMOZ4mUXe5VyKZPE7hQkIRfxtjXf3NyWZz3F7ZOKAq8Gf+I2RG8nQnHe6
O6XvKx7EsMLNv+JQWWTEqmGk+8VIWjywL3WnqK0KlWYAR6sZ6kMuSEWMPk=
-----END PUBLIC KEY-----";
        }

        public CryptoKeyDTO GenerateNewKey()
        {
            var expiration = DateTime.UtcNow.AddDays(10);
            var keyId = Guid.NewGuid().ToString();
            var privateKey = GetTestPrivateKey();
            var publicKeyPem = GetTestPublicKeyPem();

            return new CryptoKeyDTO()
            {
                Expiration = expiration,
                KeyId = keyId,
                PrivateKey = privateKey,
                PublicKeyPem = publicKeyPem
            };
        }

        public bool IsExpired(string keyId)
        {
            return false;
        }
    }
}
