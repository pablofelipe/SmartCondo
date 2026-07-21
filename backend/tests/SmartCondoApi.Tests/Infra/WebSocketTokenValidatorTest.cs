using SmartCondoApi.Infra;
using SmartCondoApi.Tests.Helpers;

namespace SmartCondoApi.Tests.Infra
{
    [TestClass]
    public class WebSocketTokenValidatorTest
    {
        private static string ValidToken(string subject = "42", DateTime? expires = null)
        {
            var configuration = TestHelper.CreateConfiguration();
            var tokenHandler = new TokenHandler(configuration);
            return tokenHandler.Generate(subject, "resident@example.com", "Resident", expires ?? DateTime.UtcNow.AddMinutes(5));
        }

        [TestMethod]
        public void TryGetUserId_ValidToken_ReturnsTrueAndTheSubjectAsUserId()
        {
            var configuration = TestHelper.CreateConfiguration();

            var result = WebSocketTokenValidator.TryGetUserId(ValidToken("42"), configuration, out var userId);

            Assert.IsTrue(result);
            Assert.AreEqual(42, userId);
        }

        [TestMethod]
        public void TryGetUserId_NullToken_ReturnsFalse()
        {
            var configuration = TestHelper.CreateConfiguration();

            var result = WebSocketTokenValidator.TryGetUserId(null, configuration, out _);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TryGetUserId_TamperedToken_ReturnsFalse()
        {
            var configuration = TestHelper.CreateConfiguration();
            var token = ValidToken("42");
            var tampered = token[..^3] + "xyz";

            var result = WebSocketTokenValidator.TryGetUserId(tampered, configuration, out _);

            Assert.IsFalse(result);
        }

        [TestMethod]
        public void TryGetUserId_ExpiredToken_ReturnsFalse()
        {
            var configuration = TestHelper.CreateConfiguration();
            var expired = ValidToken("42", DateTime.UtcNow.AddMinutes(-5));

            var result = WebSocketTokenValidator.TryGetUserId(expired, configuration, out _);

            Assert.IsFalse(result);
        }
    }
}
