using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Moq;
using SmartCondoApi.Controllers;
using SmartCondoApi.Dto;
using System.Security.Claims;

namespace SmartCondoApi.Tests.Controllers
{
    [TestClass]
    public class MessagesControllerTest : BaseControllerTest
    {
        [TestInitialize]
        public async Task Initialize()
        {
            await InitializeBase();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context?.Dispose();
        }

        private async Task<OkObjectResult> PerformSuccessSendMessage(ClaimsPrincipal sender, MessageCreateDto fakeMessageCreateDto)
        {
            var loggerMock = new Mock<ILogger<MessagesController>>();

            MessagesController _controller = new(_messageService, _notificationService, _userManager, _context, loggerMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext { User = sender }
                }
            };

            var result = await _controller.SendMessage(fakeMessageCreateDto);

            OkObjectResult? okObjectResult = SuccessAssert(result);

            return okObjectResult;
        }

        [TestMethod]
        public async Task SendMessageToCondoAdminSuccess()
        {
            Console.WriteLine("SendMessageToCondoAdminSuccess begin");

            var sender = new ClaimsPrincipal(new ClaimsIdentity(
            [
            new Claim(ClaimTypes.NameIdentifier, "2" /*"adminA@aaa.com"*/)
            ]));

            var fakeMessageCreateDto = new MessageCreateDto
            {
                Content = "This is a sample content",
                Scope = Models.MessageScope.Individual,
                CondominiumId = 1,
                FloorId = 0,
                RecipientUserId = 3,
                TowerId = 0
            };

            var result = await PerformSuccessSendMessage(sender, fakeMessageCreateDto);
            Console.WriteLine("SendMessageToCondoAdminSuccess: " + result.Value);
        }

    }
}
