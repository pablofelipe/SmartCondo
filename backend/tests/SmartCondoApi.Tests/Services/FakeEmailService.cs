using Microsoft.Extensions.Configuration;
using SmartCondoApi.Services.Email;

namespace SmartCondoApi.Tests.Services
{
    public class FakeEmailService(IConfiguration configuration) : IEmailService
    {
        public Task SendEmailAsync(string email, string subject, string message)
        {
            return Task.CompletedTask;
        }
    }
}
