using Microsoft.AspNetCore.Mvc;
using SmartCondoApi.Controllers;
using SmartCondoApi.Dto;

namespace SmartCondoApi.Tests.Controllers
{
    [TestClass]
    public sealed class AuthControllerTest : BaseControllerTest
    {
        [TestInitialize]
        public async Task InitializeAsync()
        {
            await InitializeBase();
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context?.Dispose();
        }

        private async Task<OkObjectResult> PerformSuccessLoginTest(string email, string password)
        {
            var result = await DispatchLogin(email, password);

            OkObjectResult? okObjectResult = SuccessAssert(result);

            return okObjectResult;
        }


        private async Task<UnauthorizedObjectResult> PerformUnauthorizedLoginTest(string email, string password, string message)
        {
            var result = await DispatchLogin(email, password);

            UnauthorizedObjectResult? objectResult = UnauthorizedAssert(result, message);

            return objectResult;
        }
        private static UnauthorizedObjectResult? UnauthorizedAssert(ActionResult result, string message)
        {
            Assert.IsNotNull(result, "O resultado não deve ser nulo.");
            var objectResult = result as UnauthorizedObjectResult;
            Assert.IsNotNull(objectResult, "O resultado deve ser do tipo UnauthorizedObjectResult.");
            Assert.IsNotNull(objectResult.Value, "O valor retornado não deve ser nulo.");
            Assert.AreEqual(401, objectResult.StatusCode, "O status code deve ser 401.");
            Assert.IsTrue(objectResult.Value.ToString().Contains(message), $"A Mensagem deve possuir {message}");
            return objectResult;
        }

        private async Task<NotFoundObjectResult> PerformNotFoundLoginTest(string email, string password, string message)
        {
            var result = await DispatchLogin(email, password);

            NotFoundObjectResult? objectResult = NotFoundAssert(result, message);

            return objectResult;
        }

        private static NotFoundObjectResult? NotFoundAssert(ActionResult result, string message)
        {
            Assert.IsNotNull(result, "O resultado não deve ser nulo.");
            var objectResult = result as NotFoundObjectResult;
            Assert.IsNotNull(objectResult, "O resultado deve ser do tipo NotFoundObjectResult.");
            Assert.IsNotNull(objectResult.Value, "O valor retornado não deve ser nulo.");
            Assert.AreEqual(404, objectResult.StatusCode, "O status code deve ser 404.");
            Assert.IsTrue(objectResult.Value.ToString().Contains(message), $"A Mensagem deve possuir {message}");
            return objectResult;
        }

        private async Task<BadRequestObjectResult> PerformBadRequestObjectLoginTest(string email, string password, string message)
        {
            var result = await DispatchLogin(email, password);

            BadRequestObjectResult? objectResult = BadRequestAssert(result, message);

            return objectResult;
        }

        private static BadRequestObjectResult? BadRequestAssert(ActionResult result, string message)
        {
            Assert.IsNotNull(result, "O resultado não deve ser nulo.");
            var objectResult = result as BadRequestObjectResult;
            Assert.IsNotNull(objectResult, "O resultado deve ser do tipo BadRequestObjectResult.");
            Assert.IsNotNull(objectResult.Value, "O valor retornado não deve ser nulo.");
            Assert.AreEqual(400, objectResult.StatusCode, "O status code deve ser 400.");
            Assert.IsTrue(objectResult.Value.ToString().Contains(message), $"A Mensagem deve possuir {message}");
            return objectResult;
        }

        private async Task<ActionResult> DispatchLogin(string email, string password)
        {
            AuthController authController = LoadAuthController();

            var body = new Dictionary<string, string>
            {
                ["user"] = email,
                ["secret"] = password
            };

            var result = await authController.Login(body);
            return result;
        }

        [TestMethod]
        public async Task LoginSysAdminSuccess()
        {
            Console.WriteLine("LoginSysAdminSuccess begin");
            var result = await PerformSuccessLoginTest("adminsystem@smartcondohub.com", "Aa@123!");
            Console.WriteLine("LoginSysAdminSuccess: " + result.Value);
        }

        [TestMethod]
        public async Task LoginCondoAdminSuccess()
        {
            Console.WriteLine("LoginCondoAdminSuccess begin");
            var result = await PerformSuccessLoginTest("adminA@aaa.com", "Bb@321!");
            Console.WriteLine("LoginCondoAdminSuccess: " + result.Value);
        }

        [TestMethod]
        public async Task LoginResidentSuccess()
        {
            Console.WriteLine("LoginResidentSuccess begin");
            var result = await PerformSuccessLoginTest("resident001@aaa.com", "Cc@456!");
            Console.WriteLine("LoginResidentSuccess: " + result.Value);
        }

        [TestMethod]
        public async Task LoginWithoutEmail()
        {
            Console.WriteLine("LoginWithoutEmail begin");
            var result = await PerformBadRequestObjectLoginTest(null, "Cc@456!", "Email é obrigatório.");
            Console.WriteLine("LoginWithoutEmail: " + result.Value);
        }

        [TestMethod]
        public async Task LoginWithoutSecret()
        {
            Console.WriteLine("LoginWithoutEmail begin");
            var result = await PerformBadRequestObjectLoginTest("user@aaa.com", null, "Senha é obrigatória.");
            Console.WriteLine("LoginWithoutEmail: " + result.Value);
        }

        [TestMethod]
        public async Task LoginNotFound()
        {
            Console.WriteLine("LoginNotFound begin");
            var result = await PerformNotFoundLoginTest("ksldf@sdfsf.com", "xyz@!2354", "Usuário não encontrado.");
            Console.WriteLine("LoginNotFound: " + result.Value);
        }

        [TestMethod]
        public async Task LoginDisabled()
        {
            Console.WriteLine("LoginDisabled begin");
            var result = await PerformUnauthorizedLoginTest("resident002@aaa.com", "Cc@466!", "Usuário desabilitado.");
            Console.WriteLine("LoginDisabled: " + result.Value);
        }

        [TestMethod]
        public async Task LoginCondominiumDisabled()
        {
            Console.WriteLine("LoginCondominiumDisabled begin");
            var result = await PerformUnauthorizedLoginTest("resident009@bbb.com", "Ee@789!", "O condomínio deste usuário está desabilitado.");
            Console.WriteLine("LoginCondominiumDisabled: " + result.Value);
        }

        [TestMethod]
        public async Task LoginWrongPassword()
        {
            Console.WriteLine("LoginWrongPassword begin");
            var result = await PerformUnauthorizedLoginTest("resident001@aaa.com", "Cc6266!", "Senha incorreta.");
            Console.WriteLine("LoginWrongPassword: " + result.Value);
        }

        [TestMethod]
        public async Task LoginUserProfileNotFound()
        {
            Console.WriteLine("LoginUserProfileNotFound begin");
            var result = await PerformNotFoundLoginTest("resident003@aaa.com", "Dd6266!", "Perfil de usuário não encontrado");
            Console.WriteLine("LoginUserProfileNotFound: " + result.Value);
        }

        [TestMethod]
        public async Task LoginUserTypeNotFound()
        {
            Console.WriteLine("LoginUserTypeNotFound begin");
            var result = await PerformNotFoundLoginTest("unknowUserType01@aaa.com", "V6!]Bq3;", "Tipo de Usuário não encontrado.");
            Console.WriteLine("LoginUserTypeNotFound: " + result.Value);
        }

        [TestMethod]
        public async Task EmailUnconfirmed()
        {
            Console.WriteLine("EmailUnconfirmed begin");
            var result = await PerformUnauthorizedLoginTest("resident005@aaa.com", "W8bnw#", "O email não foi validado");
            Console.WriteLine("EmailUnconfirmed: " + result.Value);
        }
    }
}
