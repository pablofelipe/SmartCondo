using Amazon.ApiGatewayManagementApi;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using SmartCondoApi.Controllers;
using SmartCondoApi.Dto;
using SmartCondoApi.Models;
using SmartCondoApi.Services.Auth;
using SmartCondoApi.Services.Crypto;
using SmartCondoApi.Services.Email;
using SmartCondoApi.Services.LinkGenerator;
using SmartCondoApi.Services.Message;
using SmartCondoApi.Services.Notification;
using SmartCondoApi.Services.User;
using SmartCondoApi.Tests.Helpers;
using SmartCondoApi.Tests.Services;

namespace SmartCondoApi.Tests.Controllers
{
    public abstract class BaseControllerTest
    {
        protected SmartCondoContext _context;
        protected UserManager<User> _userManager;
        protected IConfiguration _configuration;
        protected IMessageService _messageService;
        protected INotificationService _notificationService;
        protected ICryptoService _cryptoService;

        [TestInitialize]
        protected async Task InitializeBase()
        {
            _context = TestHelper.SeedTestData();

            _userManager = await TestHelper.CreateUserManagerAsync(_context);

            _configuration = TestHelper.CreateConfiguration();

            var loggerMock = new Mock<ILogger<MessageService>>();

            _messageService = new MessageService(_context, loggerMock.Object);

            _notificationService = new NotificationService(_context, new Mock<IAmazonApiGatewayManagementApi>().Object);


            _cryptoService = new CryptoServiceTests();
        }

        protected static OkObjectResult SuccessAssert(ActionResult result)
        {
            Assert.IsNotNull(result, "O resultado não deve ser nulo.");
            var okObjectResult = result as OkObjectResult;
            Assert.IsNotNull(okObjectResult, "O resultado deve ser do tipo OkObjectResult.");
            Assert.IsNotNull(okObjectResult.Value, "O valor retornado não deve ser nulo.");
            Assert.AreEqual(200, okObjectResult.StatusCode, "O status code deve ser 200.");
            return okObjectResult;
        }

        protected AuthController LoadAuthController()
        {
            var dependencies = new AuthDependencies(_context, _userManager, _configuration, _cryptoService);

            var userService = new AuthService(dependencies);

            var loggerMock = new Mock<ILogger<AuthController>>();

            var authController = new AuthController(userService, loggerMock.Object);

            return authController;
        }

        private static IConfigurationRoot GetConfiguration()
        {
            return new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                .Build();
        }

        protected UserProfileController LoadUserProfileController()
        {
            IConfiguration mockConfiguration = GetConfiguration();
            var emailService = new EmailServiceTests(mockConfiguration);
            var emailConfService = new EmailConfirmationService(_userManager);

            var mockHttpContext = new Mock<HttpContext>();
            var mockHttpContextAccessor = new Mock<IHttpContextAccessor>();

            var mockLinkGeneratorService = new Mock<ILinkGeneratorService>();
            LinkGenerator linkGenerator;

            mockLinkGeneratorService
                .Setup(s => s.GenerateConfirmationLink(
                    It.IsAny<string>(), // action
                    It.IsAny<string>(), // controller
                    It.IsAny<object>()  // values
                ))
                .Returns((string action, string controller, object values) =>
                {
                    var userId = values.GetType().GetProperty("userId")?.GetValue(values)?.ToString();
                    var token = values.GetType().GetProperty("token")?.GetValue(values)?.ToString();
                    return $"https://smartcondocli.com/api/auth/confirm-email?userId={userId}&token={token}";
                });


            var mockRequest = new Mock<HttpRequest>();
            mockRequest.Setup(r => r.Scheme).Returns("https");
            mockRequest.Setup(r => r.Host).Returns(new HostString("localhost", 5254));
            mockHttpContext.Setup(c => c.Request).Returns(mockRequest.Object);

            var userProfileServiceDependencies = new UserProfileServiceDependencies(_context, _userManager, _cryptoService);

            var loggerMockService = new Mock<ILogger<UserProfileService>>();

            var userService = new UserProfileService(userProfileServiceDependencies, loggerMockService.Object);

            var loggerMockController = new Mock<ILogger<UserProfileController>>();

            var userProfileControllerDependencies = new UserProfileControllerDependencies(userService, mockLinkGeneratorService.Object, emailService, emailConfService, loggerMockController.Object);

            return new UserProfileController(userProfileControllerDependencies);
        }
    }
}
