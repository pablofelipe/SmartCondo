using System.Net;
using System.Net.Mail;

namespace SmartCondoApi.Services.Email
{
    public sealed record SmtpSettings(string Server, int Port, string FromEmail, string FromPassword, bool EnableSsl);

    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public static SmtpSettings ResolveSmtpSettings(IConfiguration configuration)
        {
            var server = RequireSetting(configuration, "SmtpServer");
            var portValue = RequireSetting(configuration, "SmtpPort");
            var fromEmail = RequireSetting(configuration, "FromEmail");
            var fromPassword = RequireSetting(configuration, "FromPassword");
            var enableSslValue = RequireSetting(configuration, "EnableSsl");

            if (!int.TryParse(portValue, out var port))
                throw new InvalidOperationException($"EmailSettings:SmtpPort is not a valid port number: '{portValue}'");

            if (!bool.TryParse(enableSslValue, out var enableSsl))
                throw new InvalidOperationException($"EmailSettings:EnableSsl is not a valid boolean: '{enableSslValue}'");

            return new SmtpSettings(server, port, fromEmail, fromPassword, enableSsl);
        }

        private static string RequireSetting(IConfiguration configuration, string key)
        {
            var value = configuration[$"EmailSettings:{key}"];
            return string.IsNullOrEmpty(value)
                ? throw new InvalidOperationException($"EmailSettings:{key} is missing")
                : value;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            var settings = ResolveSmtpSettings(_configuration);

            using var client = new SmtpClient(settings.Server, settings.Port)
            {
                Credentials = new NetworkCredential(settings.FromEmail, settings.FromPassword),
                EnableSsl = settings.EnableSsl,
                DeliveryMethod = SmtpDeliveryMethod.Network,
                UseDefaultCredentials = false,
                Timeout = 10000
            };

            using var mailMessage = new MailMessage(settings.FromEmail, email, subject, message)
            {
                IsBodyHtml = true
            };

            try
            {
                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Email sent to {Recipient}", email);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send email to {Recipient}", email);
            }
        }
    }
}
