using Amazon.SimpleEmail;
using Amazon.SimpleEmail.Model;
using System.Net;
using System.Net.Mail;
using System.Net.Sockets;

namespace SmartCondoApi.Services.Email
{
    public class EmailService : IEmailService
    {
        private readonly ILogger<EmailService> _logger;
        private readonly IConfiguration _configuration;

        public EmailService(IConfiguration configuration, ILogger<EmailService> logger)
        {
            _configuration = configuration;
            _logger = logger;
        }

        public async Task SendEmailAsync(string email, string subject, string message)
        {
            //await SendGmailAsync(email, subject, message);
            await SendEmailSesAsync(email, subject, message);
        }

        public async Task<bool> SendEmailSesAsync(string to, string subject, string body)
        {
            const int timeoutSeconds = 20;
            try
            {
                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPort = _configuration["EmailSettings:SmtpPort"];
                var enableSsl = _configuration["EmailSettings:EnableSsl"];

                var config = new AmazonSimpleEmailServiceConfig
                {
                    RegionEndpoint = Amazon.RegionEndpoint.SAEast1, // São Paulo
                    Timeout = TimeSpan.FromSeconds(timeoutSeconds)
                };

                using var sesClient = new AmazonSimpleEmailServiceClient(config);

                _logger.LogDebug("Config: Server={Server}, Port={Port}, SSL={Ssl}",
                    smtpServer, smtpPort, enableSsl);

                _logger.LogDebug("Config: FromEmail={fromEmail}, to={to}, subject={subject}, body={body}",
                    fromEmail, to, subject, body);

                var request = new SendEmailRequest
                {
                    Source = fromEmail,
                    Destination = new Destination { ToAddresses = [to] },
                    Message = new Amazon.SimpleEmail.Model.Message
                    {
                        Subject = new Content(subject),
                        Body = new Body { Html = new Content(body) }
                    }
                };

                _logger.LogDebug("Enviando email via SES");
                var response = await sesClient.SendEmailAsync(request);
                _logger.LogInformation("Email enviado via SES: {MessageId}", response.MessageId);
                return true;
            }
            catch (TaskCanceledException)
            {
                _logger.LogError($"TIMEOUT - SES não respondeu em {timeoutSeconds} segundos");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao enviar email via SES");
                return false;
            }
        }

        public async Task SendGmailAsync(string email, string subject, string message)
        {
            try
            {
                _logger.LogDebug("INICIANDO DEBUG DETALHADO SMTP");

                var smtpServer = _configuration["EmailSettings:SmtpServer"];
                var smtpPort = _configuration["EmailSettings:SmtpPort"];
                var fromEmail = _configuration["EmailSettings:FromEmail"];
                var fromPassword = _configuration["EmailSettings:FromPassword"];
                var enableSsl = _configuration["EmailSettings:EnableSsl"];

                _logger.LogDebug("Config: Server={Server}, Port={Port}, Email={Email}, SSL={Ssl}",
                    smtpServer, smtpPort, fromEmail, enableSsl);

                // Teste de conexão básica
                /*
                _logger.LogDebug("Testando conexão TCP...");
                using var tcpClient = new TcpClient();
                await tcpClient.ConnectAsync(smtpServer, Convert.ToInt32(smtpPort));
                _logger.LogDebug("Conexão TCP bem-sucedida");
                tcpClient.Close();
                */

                // Configuração SMTP
                using var client = new SmtpClient(smtpServer, Convert.ToInt32(smtpPort))
                {
                    Credentials = new NetworkCredential(fromEmail, fromPassword),
                    EnableSsl = Convert.ToBoolean(enableSsl),
                    DeliveryMethod = SmtpDeliveryMethod.Network,
                    UseDefaultCredentials = false,
                    Timeout = 10000
                };

                _logger.LogDebug("Cliente SMTP configurado. Tentando enviar...");

                var mailMessage = new MailMessage(fromEmail, email, subject, message)
                {
                    IsBodyHtml = true
                };

                await client.SendMailAsync(mailMessage);
                _logger.LogInformation("Email enviado com sucesso!");
            }
            catch (SocketException sockEx)
            {
                _logger.LogError(sockEx, "ERRO DE SOCKET - Não conseguiu conectar no Gmail");
            }
            catch (SmtpException smtpEx)
            {
                _logger.LogError(smtpEx, "ERRO SMTP DETALHADO - Status: {Status}, Message: {Message}",
                    smtpEx.StatusCode, smtpEx.Message);

                if (smtpEx.InnerException != null)
                {
                    _logger.LogError(smtpEx.InnerException, "INNER EXCEPTION do SMTP");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ERRO INESPERADO");
            }
        }

        private async Task<bool> TestSmtpConnectionAsync(string server, int port)
        {
            try
            {
                using var client = new TcpClient();
                await client.ConnectAsync(server, port);
                _logger.LogDebug("Conexão TCP bem-sucedida com {Server}:{Port}", server, port);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Falha na conexão TCP com {Server}:{Port}", server, port);
                return false;
            }
        }
    }
}

