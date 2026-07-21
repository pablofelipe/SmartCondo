using Microsoft.Extensions.Configuration;
using SmartCondoApi.Services.Email;

namespace SmartCondoApi.Tests.Services
{
    [TestClass]
    public class EmailServiceTests
    {
        private static IConfiguration BuildConfiguration(Dictionary<string, string?> overrides)
        {
            var settings = new Dictionary<string, string?>
            {
                ["EmailSettings:SmtpServer"] = "smtp.example.com",
                ["EmailSettings:SmtpPort"] = "587",
                ["EmailSettings:FromEmail"] = "noreply@example.com",
                ["EmailSettings:FromPassword"] = "secret",
                ["EmailSettings:EnableSsl"] = "true"
            };

            foreach (var (key, value) in overrides)
                settings[key] = value;

            return new ConfigurationBuilder().AddInMemoryCollection(settings).Build();
        }

        [TestMethod]
        public void ResolveSmtpSettings_AllValuesPresent_ReturnsParsedSettings()
        {
            var configuration = BuildConfiguration([]);

            var settings = EmailService.ResolveSmtpSettings(configuration);

            Assert.AreEqual("smtp.example.com", settings.Server);
            Assert.AreEqual(587, settings.Port);
            Assert.AreEqual("noreply@example.com", settings.FromEmail);
            Assert.AreEqual("secret", settings.FromPassword);
            Assert.IsTrue(settings.EnableSsl);
        }

        [TestMethod]
        public void ResolveSmtpSettings_MissingSmtpServer_Throws()
        {
            var configuration = BuildConfiguration(new() { ["EmailSettings:SmtpServer"] = null });

            Assert.ThrowsException<InvalidOperationException>(() => EmailService.ResolveSmtpSettings(configuration));
        }

        [TestMethod]
        public void ResolveSmtpSettings_MissingFromEmail_Throws()
        {
            var configuration = BuildConfiguration(new() { ["EmailSettings:FromEmail"] = null });

            Assert.ThrowsException<InvalidOperationException>(() => EmailService.ResolveSmtpSettings(configuration));
        }

        [TestMethod]
        public void ResolveSmtpSettings_MissingFromPassword_Throws()
        {
            var configuration = BuildConfiguration(new() { ["EmailSettings:FromPassword"] = null });

            Assert.ThrowsException<InvalidOperationException>(() => EmailService.ResolveSmtpSettings(configuration));
        }

        [TestMethod]
        public void ResolveSmtpSettings_NonNumericSmtpPort_Throws()
        {
            var configuration = BuildConfiguration(new() { ["EmailSettings:SmtpPort"] = "not-a-port" });

            Assert.ThrowsException<InvalidOperationException>(() => EmailService.ResolveSmtpSettings(configuration));
        }

        [TestMethod]
        public void ResolveSmtpSettings_NonBooleanEnableSsl_Throws()
        {
            var configuration = BuildConfiguration(new() { ["EmailSettings:EnableSsl"] = "maybe" });

            Assert.ThrowsException<InvalidOperationException>(() => EmailService.ResolveSmtpSettings(configuration));
        }
    }
}
