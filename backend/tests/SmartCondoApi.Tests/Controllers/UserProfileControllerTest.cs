using Microsoft.AspNetCore.Mvc;
using SmartCondoApi.Controllers;
using SmartCondoApi.Dto;

namespace SmartCondoApi.Tests.Controllers
{
    [TestClass]
    public sealed class UserProfileControllerTest : BaseControllerTest
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

        private async Task<OkObjectResult> PerformSuccessAddUserTest(UserProfileCreateDTO userCreateDTO)
        {
            var userProfileController = LoadUserProfileController();

            var result = await userProfileController.AddUser(userCreateDTO);

            OkObjectResult? okObjectResult = SuccessAssert(result);

            await PerformConfirmEmailTest(userProfileController, okObjectResult.Value);

            return okObjectResult;
        }

        [TestMethod]
        public async Task AddUserSuccess()
        {
            Console.WriteLine("AddUserSuccess begin");

            var fakeUserCreateDTO = new UserProfileCreateDTO
            {
                Name = "Fake User",
                Address = "123 Fake Street",
                Phone1 = "6821365089",
                UserTypeId = 3,
                RegistrationNumber = "25405758540",
                CondominiumId = 1,
                TowerId = 1,
                FloorId = 1,
                Apartment = 101,
                ParkingSpaceNumber = 10,
                User = new UserCreateDTO
                {
                    Email = "test@example.com",
                    Password = "rpW$6@b2",
                    Enabled = true
                }
            };

            var result = await PerformSuccessAddUserTest(fakeUserCreateDTO);
            Console.WriteLine("AddUserSuccess: " + result.Value);
        }

        private async Task<BadRequestObjectResult> PerformBadRequestAddUserTest(UserProfileCreateDTO userCreateDTO, string message)
        {
            var userProfileController = LoadUserProfileController();

            var result = await userProfileController.AddUser(userCreateDTO);

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

        private async Task<UnauthorizedObjectResult> PerformUnauthorizedAddUserTest(UserProfileCreateDTO userCreateDTO, string message)
        {
            var userProfileController = LoadUserProfileController();

            var result = await userProfileController.AddUser(userCreateDTO);

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

        [TestMethod]
        public async Task WrongRegistrationNumber()
        {
            Console.WriteLine("WrongRegistrationNumber begin");

            var fakeUserCreateDTO = new UserProfileCreateDTO
            {
                Name = "Fake User",
                Address = "123 Fake Street",
                Phone1 = "8238405472",
                UserTypeId = 3,
                RegistrationNumber = "47847762490",
                CondominiumId = 1,
                TowerId = 1,
                FloorId = 1,
                Apartment = 101,
                ParkingSpaceNumber = 10,
                User = new UserCreateDTO
                {
                    Email = "test2@example.com",
                    Password = "SbP$Vq",
                    Enabled = true
                }
            };

            var result = await PerformBadRequestAddUserTest(fakeUserCreateDTO, "CPF/CNPJ inválido");
            Console.WriteLine("WrongRegistrationNumber: " + result.Value);
        }


        [TestMethod]
        public async Task WrongLogin()
        {
            Console.WriteLine("WrongLogin begin");

            var fakeUserCreateDTO = new UserProfileCreateDTO
            {
                Name = "Fake User",
                Address = "123 Fake Street",
                Phone1 = "8238405472",
                UserTypeId = 3,
                RegistrationNumber = "34855196185",
                CondominiumId = 1,
                TowerId = 1,
                FloorId = 1,
                Apartment = 101,
                ParkingSpaceNumber = 10
            };

            var result = await PerformBadRequestAddUserTest(fakeUserCreateDTO, "Nenhum login encontrado");
            Console.WriteLine("WrongLogin: " + result.Value);
        }

        [TestMethod]
        public async Task NotFoundCondominiumUserNeededIt()
        {
            Console.WriteLine("NotFoundCondominiumUserNeededIt begin");

            var fakeUserCreateDTO = new UserProfileCreateDTO
            {
                Name = "Fake User",
                Address = "123 Fake Street",
                Phone1 = "8238405472",
                UserTypeId = 3,
                RegistrationNumber = "90445021314",
                CondominiumId = 10,
                TowerId = 1,
                FloorId = 1,
                Apartment = 101,
                ParkingSpaceNumber = 10,
                User = new UserCreateDTO
                {
                    Email = "test3@example.com",
                    Password = "5!EagD",
                    Enabled = true
                }
            };

            var result = await PerformBadRequestAddUserTest(fakeUserCreateDTO, "Condominio 10 não encontrado");
            Console.WriteLine("NotFoundCondominiumUserNeededIt: " + result.Value);
        }

        [TestMethod]
        public async Task NotFoundCondominiumUserNotNeededIt()
        {
            Console.WriteLine("NotFoundCondominiumUserNotNeededIt begin");

            var fakeUserCreateDTO = new UserProfileCreateDTO
            {
                Name = "Fake User",
                Address = "123 Fake Street",
                Phone1 = "9726442868",
                UserTypeId = 1,
                RegistrationNumber = "11453868739",
                CondominiumId = 0,
                TowerId = 1,
                FloorId = 1,
                Apartment = 101,
                ParkingSpaceNumber = 10,
                User = new UserCreateDTO
                {
                    Email = "test4@example.com",
                    Password = "T3GT+L",
                    Enabled = true
                }
            };

            var result = await PerformSuccessAddUserTest(fakeUserCreateDTO);
            Console.WriteLine("NotFoundCondominiumUserNotNeededIt: " + result.Value);
        }

        [TestMethod]
        public async Task CondominiumDisabled()
        {
            Console.WriteLine("CondominiumDisabled begin");

            var fakeUserCreateDTO = new UserProfileCreateDTO
            {
                Name = "Fake User",
                Address = "123 Fake Street",
                Phone1 = "3721976624",
                UserTypeId = 2,
                RegistrationNumber = "87091871626",
                CondominiumId = 2,
                TowerId = 1,
                FloorId = 1,
                Apartment = 101,
                ParkingSpaceNumber = 10,
                User = new UserCreateDTO
                {
                    Email = "test5@example.com",
                    Password = "T!VOt6",
                    Enabled = true
                }
            };

            var result = await PerformUnauthorizedAddUserTest(fakeUserCreateDTO, "Condomínio Condominium B desabilitado");
            Console.WriteLine("CondominiumDisabled: " + result.Value);
        }

        [TestMethod]
        public async Task MaxUsersByCondominium()
        {
            Console.WriteLine("MaxUsersByCondominium begin");

            var fakeUserCreateDTO1 = new UserProfileCreateDTO
            {
                Name = "Fake User",
                Address = "123 Fake Street",
                Phone1 = "7538643135",
                UserTypeId = 3,
                RegistrationNumber = "90691049386",
                CondominiumId = 3,
                TowerId = 4,
                FloorId = 1,
                Apartment = 101,
                ParkingSpaceNumber = 10,
                User = new UserCreateDTO
                {
                    Email = "test6@example.com",
                    Password = "f6FQo+",
                    Enabled = true
                }
            };

            var result1 = await PerformSuccessAddUserTest(fakeUserCreateDTO1);
            Console.WriteLine("MaxUsersByCondominium: " + result1.Value);

            var fakeUserCreateDTO2 = new UserProfileCreateDTO
            {
                Name = "Fake User",
                Address = "123 Fake Street",
                Phone1 = "8321386150",
                UserTypeId = 3,
                RegistrationNumber = "54755371600",
                CondominiumId = 3,
                TowerId = 4,
                FloorId = 1,
                Apartment = 101,
                ParkingSpaceNumber = 10,
                User = new UserCreateDTO
                {
                    Email = "test7@example.com",
                    Password = "!Olpj2",
                    Enabled = true
                }
            };

            var result2 = await PerformSuccessAddUserTest(fakeUserCreateDTO2);
            Console.WriteLine("MaxUsersByCondominium: " + result2.Value);

            var fakeUserCreateDTO3 = new UserProfileCreateDTO
            {
                Name = "Fake User",
                Address = "123 Fake Street",
                Phone1 = "7936424636",
                UserTypeId = 3,
                RegistrationNumber = "37905291685",
                CondominiumId = 3,
                TowerId = 4,
                FloorId = 1,
                Apartment = 101,
                ParkingSpaceNumber = 10,
                User = new UserCreateDTO
                {
                    Email = "test8@example.com",
                    Password = "R7(b5C",
                    Enabled = true
                }
            };

            var result3 = await PerformUnauthorizedAddUserTest(fakeUserCreateDTO3, "O número máximo de usuários permitidos para este condomínio foi atingido");

            Console.WriteLine("MaxUsersByCondominium: " + result3.Value);
        }

        private async Task PerformConfirmEmailTest(UserProfileController userProfileController, object resultValue)
        {
            UserProfileResponseDTO userProfile = (UserProfileResponseDTO)resultValue;

            var result = await userProfileController.ConfirmEmail(userProfile.Id.ToString(), userProfile.Token);

            SuccessAssert(result);
        }

        [TestMethod]
        public async Task UsertTypeNotFound()
        {
            Console.WriteLine("UsertTypeNotFound begin");

            var fakeUserCreateDTO1 = new UserProfileCreateDTO
            {
                Name = "Fake User",
                Address = "123 Fake Street",
                Phone1 = "6735077532",
                UserTypeId = 99,
                RegistrationNumber = "79562594289",
                CondominiumId = 1,
                TowerId = 1,
                FloorId = 1,
                Apartment = 101,
                ParkingSpaceNumber = 10,
                User = new UserCreateDTO
                {
                    Email = "test9@example.com",
                    Password = "zr$fu@",
                    Enabled = true
                }
            };

            var result1 = await PerformBadRequestAddUserTest(fakeUserCreateDTO1, "Tipo de usuário 99 não encontrado");

            Console.WriteLine("UsertTypeNotFound: " + result1.Value);
        }

        [TestMethod]
        public async Task WrongApartment()
        {
            Console.WriteLine("WrongApartment begin");

            var fakeUserCreateDTO1 = new UserProfileCreateDTO
            {
                Name = "Fake User",
                Address = "123 Fake Street",
                Phone1 = "2727667020",
                UserTypeId = 3,
                RegistrationNumber = "74661168178",
                CondominiumId = 1,
                TowerId = 1,
                FloorId = 1,
                Apartment = 0,
                ParkingSpaceNumber = 10,
                User = new UserCreateDTO
                {
                    Email = "test10@example.com",
                    Password = "v+jhpw",
                    Enabled = true
                }
            };

            var result1 = await PerformBadRequestAddUserTest(fakeUserCreateDTO1, "Número de apartamento incorreto");

            Console.WriteLine("WrongApartment: " + result1.Value);
        }

        [TestMethod]
        public async Task WrongTowerByNumber()
        {
            Console.WriteLine("WrongTowerByNumber begin");

            var fakeUserCreateDTO1 = new UserProfileCreateDTO
            {
                Name = "Fake User",
                Address = "123 Fake Street",
                Phone1 = "8833721857",
                UserTypeId = 3,
                RegistrationNumber = "78352638408",
                CondominiumId = 3,
                TowerId = 2,
                FloorId = 1,
                Apartment = 12,
                ParkingSpaceNumber = 10,
                User = new UserCreateDTO
                {
                    Email = "test12@example.com",
                    Password = "S@REe!",
                    Enabled = true
                }
            };

            var result1 = await PerformBadRequestAddUserTest(fakeUserCreateDTO1, "Torre 2 não encontrada");

            Console.WriteLine("WrongTowerByNumber: " + result1.Value);
        }

        [TestMethod]
        public async Task WrongFloorByCount()
        {
            Console.WriteLine("WrongFloorByCount begin");

            var fakeUserCreateDTO1 = new UserProfileCreateDTO
            {
                Name = "Fake User",
                Address = "123 Fake Street",
                Phone1 = "5524863977",
                UserTypeId = 3,
                RegistrationNumber = "11367858097",
                CondominiumId = 1,
                TowerId = 1,
                FloorId = 5,
                Apartment = 55,
                ParkingSpaceNumber = 10,
                User = new UserCreateDTO
                {
                    Email = "test13@example.com",
                    Password = "D8By*I",
                    Enabled = true
                }
            };

            var result1 = await PerformBadRequestAddUserTest(fakeUserCreateDTO1, "Número de andar incorreto. A torre Tower A1 possui 4 andar(es)");

            Console.WriteLine("WrongFloorByCount: " + result1.Value);
        }


        [TestMethod]
        public async Task WrongParkingSpaceNumber()
        {
            Console.WriteLine("WrongParkingSpaceNumber begin");

            var fakeUserCreateDTO1 = new UserProfileCreateDTO
            {
                Name = "Fake User",
                Address = "123 Fake Street",
                Phone1 = "8428188216",
                UserTypeId = 3,
                RegistrationNumber = "17110711021",
                CondominiumId = 1,
                TowerId = 1,
                FloorId = 2,
                Apartment = 22,
                ParkingSpaceNumber = 20,
                User = new UserCreateDTO
                {
                    Email = "test14@example.com",
                    Password = "@Zh9Hc",
                    Enabled = true
                }
            };

            var result1 = await PerformSuccessAddUserTest(fakeUserCreateDTO1);

            var fakeUserCreateDTO2 = new UserProfileCreateDTO
            {
                Name = "Fake User",
                Address = "123 Fake Street",
                Phone1 = "9521074542",
                UserTypeId = 3,
                RegistrationNumber = "70624863611",
                CondominiumId = 1,
                TowerId = 1,
                FloorId = 3,
                Apartment = 33,
                ParkingSpaceNumber = 20,
                User = new UserCreateDTO
                {
                    Email = "test15@example.com",
                    Password = "*XvH#U",
                    Enabled = true
                }
            };

            var result2 = await PerformUnauthorizedAddUserTest(fakeUserCreateDTO2, "Número de vaga especificada já está em uso para outro apartamento");

            Console.WriteLine("WrongParkingSpaceNumber: " + result2.Value);
        }

        [TestMethod]
        public async Task LoginAlreadyExists()
        {
            Console.WriteLine("LoginAlreadyExists begin");

            var fakeUserCreateDTO = new UserProfileCreateDTO
            {
                Name = "Fake User",
                Address = "123 Fake Street",
                Phone1 = "3721976624",
                UserTypeId = 2,
                RegistrationNumber = "83477267877",
                CondominiumId = 1,
                TowerId = 1,
                FloorId = 1,
                Apartment = 101,
                ParkingSpaceNumber = 10,
                User = new UserCreateDTO
                {
                    Email = "adminsystem@smartcondohub.com",
                    Password = "T!VOt6",
                    Enabled = true
                }
            };

            var result = await PerformUnauthorizedAddUserTest(fakeUserCreateDTO, "Login adminsystem@smartcondohub.com já cadastrado");
            Console.WriteLine("LoginAlreadyExists: " + result.Value);
        }


        [TestMethod]
        public async Task UserAlreadyExists()
        {
            Console.WriteLine("UserAlreadyExists begin");

            var fakeUserCreateDTO = new UserProfileCreateDTO
            {
                Name = "Fake User",
                Address = "123 Fake Street",
                Phone1 = "3721976624",
                UserTypeId = 2,
                RegistrationNumber = "51983777404",
                CondominiumId = 1,
                TowerId = 1,
                FloorId = 1,
                Apartment = 101,
                ParkingSpaceNumber = 10,
                User = new UserCreateDTO
                {
                    Email = "adminA@bbb.com",
                    Password = "T!VOt6",
                    Enabled = true
                }
            };

            var result = await PerformUnauthorizedAddUserTest(fakeUserCreateDTO, "CPF 51983777404 já cadastrado");
            Console.WriteLine("UserAlreadyExists: " + result.Value);
        }

        [TestMethod]
        public async Task AddUserEmailSuccess()
        {
            Console.WriteLine("AddUserEmailSuccess begin");

            var fakeUserCreateDTO = new UserProfileCreateDTO
            {
                Name = "Smart Condo QA",
                Address = "507 Celso Street",
                Phone1 = "1235551533",
                UserTypeId = 2,
                RegistrationNumber = "72222468191",
                CondominiumId = 1,
                TowerId = 1,
                FloorId = 1,
                Apartment = 131,
                ParkingSpaceNumber = 100,
                User = new UserCreateDTO
                {
                    Email = "smartcondoqa@gmail.com",
                    Password = "zrT8-3fy",
                    Enabled = true
                }
            };

            var result = await PerformSuccessAddUserTest(fakeUserCreateDTO);
            Console.WriteLine("AddUserEmailSuccess: " + result.Value);
        }


        [TestMethod]
        public async Task AddUserCompanyRegistrationNumberSuccess()
        {
            Console.WriteLine("AddUserCompanyRegistrationNumberSuccess begin");

            var fakeUserCreateDTO = new UserProfileCreateDTO
            {
                Name = "Smart Condo QA2",
                Address = "507 Celso Street",
                Phone1 = "6338973915",
                UserTypeId = 2,
                RegistrationNumber = "55881929000169",
                CondominiumId = 1,
                TowerId = 1,
                FloorId = 1,
                Apartment = 131,
                ParkingSpaceNumber = 100,
                User = new UserCreateDTO
                {
                    Email = "smartcondoqa2@gmail.com",
                    Password = "s}2jBr",
                    Enabled = true
                }
            };

            var result = await PerformSuccessAddUserTest(fakeUserCreateDTO);
            Console.WriteLine("AddUserCompanyRegistrationNumberSuccess: " + result.Value);
        }
    }
}
