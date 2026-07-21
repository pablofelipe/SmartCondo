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

            MessagesController _controller = new(_messageService, _notificationService, loggerMock.Object)
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
            new Claim(ClaimTypes.NameIdentifier, "2" /*"adminA@aaa.com"*/),
            new Claim(ClaimTypes.Role, "CondominiumAdministrator")
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

        [TestMethod]
        public async Task SendMessage_UnexpectedException_ReturnsGenericMessageWithoutLeakingDetails()
        {
            var loggerMock = new Mock<ILogger<MessagesController>>();

            MessagesController controller = new(_messageService, _notificationService, loggerMock.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(
                        [
                            new Claim(ClaimTypes.NameIdentifier, "1"),
                            new Claim(ClaimTypes.Role, "SystemAdministrator")
                        ]))
                    }
                }
            };

            // SystemAdministrator (id 1) has no CondominiumId of its own, and this DTO omits one too -
            // MessageService.ResolveCondominiumId throws InvalidOperationException, which is not caught
            // by any of the controller's specific catch blocks and falls through to the generic one.
            var messageDto = new MessageCreateDto
            {
                Content = "Broadcast without a condominium",
                Scope = Models.MessageScope.Tower
            };

            var result = await controller.SendMessage(messageDto);

            var objectResult = result as ObjectResult;
            Assert.IsNotNull(objectResult);
            Assert.AreEqual(500, objectResult.StatusCode);

            var body = objectResult.Value!.ToString();
            Assert.IsFalse(body!.Contains("CondominiumId must be specified"), "The 500 response must not leak the raw exception message");
        }
    }
}
