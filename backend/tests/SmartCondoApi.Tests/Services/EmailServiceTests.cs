using Microsoft.Extensions.Configuration;
using SmartCondoApi.Services.Email;

namespace SmartCondoApi.Tests.Services
{
    public class EmailServiceTests(IConfiguration configuration) : IEmailService
    {
        public Task SendEmailAsync(string email, string subject, string message)
        {
            return Task.CompletedTask;
        }

        //public async Task TestSendEmailAsync()
        //{
        //    // Arrange
        //    var mockEmailService = new Mock<IEmailService>();

        //    mockEmailService
        //        .Setup(service => service.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()))
        //        .Returns(Task.CompletedTask);

        //    // Act
        //    await mockEmailService.Object.SendEmailAsync("test@example.com", "Subject", "Message");

        //    // Assert
        //    mockEmailService.Verify(service => service.SendEmailAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Once);
        //}
    }
}
