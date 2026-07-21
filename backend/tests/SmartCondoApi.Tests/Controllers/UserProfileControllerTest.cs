using Microsoft.AspNetCore.Mvc;
using SmartCondoApi.Controllers;
using SmartCondoApi.Dto;
using SmartCondoApi.Models;

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

        private async Task<OkObjectResult> PerformSuccessAddUserTest(UserProfileCreateDTO userCreateDTO, string? callerRole = null, long callerId = 1)
        {
            var userProfileController = LoadUserProfileController(callerRole ?? "SystemAdministrator", callerId);

            var result = await userProfileController.AddUser(userCreateDTO);

            OkObjectResult? okObjectResult = SuccessAssert(result);

            await PerformConfirmEmailTest(userProfileController, okObjectResult.Value);

            return okObjectResult;
        }

        private async Task<ObjectResult> PerformForbiddenAddUserTest(UserProfileCreateDTO userCreateDTO, string? callerRole, string message, long callerId = 1)
        {
            var userProfileController = LoadUserProfileController(callerRole, callerId);

            var result = await userProfileController.AddUser(userCreateDTO);

            ObjectResult? objectResult = ForbiddenAssert(result, message);

            return objectResult;
        }

        private static ObjectResult? ForbiddenAssert(ActionResult result, string message)
        {
            Assert.IsNotNull(result, "O resultado não deve ser nulo.");
            var objectResult = result as ObjectResult;
            Assert.IsNotNull(objectResult, "O resultado deve ser do tipo ObjectResult.");
            Assert.AreEqual(403, objectResult.StatusCode, "O status code deve ser 403.");
            var problemDetails = objectResult.Value as ProblemDetails;
            Assert.IsNotNull(problemDetails, "O valor retornado deve ser um ProblemDetails.");
            Assert.IsTrue(problemDetails.Detail!.Contains(message), $"A Mensagem deve possuir {message}");
            return objectResult;
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

        [TestMethod]
        public async Task AddUserBlockedFromCreatingSystemAdministrator()
        {
            Console.WriteLine("AddUserBlockedFromCreatingSystemAdministrator begin");

            var fakeUserCreateDTO = new UserProfileCreateDTO
            {
                Name = "Fake Admin",
                Address = "123 Fake Street",
                Phone1 = "1111111111",
                UserTypeId = 1, // SystemAdministrator
                RegistrationNumber = "60340325046",
                User = new UserCreateDTO
                {
                    Email = "escalation@example.com",
                    Password = "Aa1!aaaa",
                    Enabled = true
                }
            };

            var result = await PerformForbiddenAddUserTest(fakeUserCreateDTO, "CondominiumAdministrator", "not authorized to register");
            Console.WriteLine("AddUserBlockedFromCreatingSystemAdministrator: " + result.Value);
        }

        [TestMethod]
        public async Task AddUserWithoutRegisterCapabilityIsForbidden()
        {
            Console.WriteLine("AddUserWithoutRegisterCapabilityIsForbidden begin");

            var fakeUserCreateDTO = new UserProfileCreateDTO
            {
                Name = "Fake Resident",
                Address = "123 Fake Street",
                Phone1 = "2222222222",
                UserTypeId = 3, // Resident
                RegistrationNumber = "31226291007",
                User = new UserCreateDTO
                {
                    Email = "noauth@example.com",
                    Password = "Aa1!aaaa",
                    Enabled = true
                }
            };

            var result = await PerformForbiddenAddUserTest(fakeUserCreateDTO, "Resident", "not authorized to register");
            Console.WriteLine("AddUserWithoutRegisterCapabilityIsForbidden: " + result.Value);
        }

        [TestMethod]
        public async Task AddUserWithNarrowRegisterableTypesSuccess()
        {
            Console.WriteLine("AddUserWithNarrowRegisterableTypesSuccess begin");

            var fakeUserCreateDTO = new UserProfileCreateDTO
            {
                Name = "Delegated Resident",
                Address = "123 Fake Street",
                Phone1 = "3333333333",
                UserTypeId = 3, // Resident
                RegistrationNumber = "48916144043",
                CondominiumId = 1,
                TowerId = 1,
                FloorId = 1,
                Apartment = 201,
                ParkingSpaceNumber = 30,
                User = new UserCreateDTO
                {
                    Email = "delegated@example.com",
                    Password = "Aa1!aaaa",
                    Enabled = true
                }
            };

            // Caller id must belong to a profile whose own CondominiumId matches the target (Step 4: Scope).
            var result = await PerformSuccessAddUserTest(fakeUserCreateDTO, "ResidentCommitteeMember", callerId: 2);
            Console.WriteLine("AddUserWithNarrowRegisterableTypesSuccess: " + result.Value);
        }

        [TestMethod]
        public async Task AddUserWithNarrowRegisterableTypesForbiddenOutsideList()
        {
            Console.WriteLine("AddUserWithNarrowRegisterableTypesForbiddenOutsideList begin");

            var fakeUserCreateDTO = new UserProfileCreateDTO
            {
                Name = "Fake Admin",
                Address = "123 Fake Street",
                Phone1 = "4444444444",
                UserTypeId = 2, // CondominiumAdministrator
                RegistrationNumber = "77817120056",
                User = new UserCreateDTO
                {
                    Email = "escalation2@example.com",
                    Password = "Aa1!aaaa",
                    Enabled = true
                }
            };

            var result = await PerformForbiddenAddUserTest(fakeUserCreateDTO, "ResidentCommitteeMember", "not authorized to register");
            Console.WriteLine("AddUserWithNarrowRegisterableTypesForbiddenOutsideList: " + result.Value);
        }

        [TestMethod]
        public async Task AddUserWithUnknownCallerRoleIsForbidden()
        {
            Console.WriteLine("AddUserWithUnknownCallerRoleIsForbidden begin");

            var fakeUserCreateDTO = new UserProfileCreateDTO
            {
                Name = "Fake Resident",
                Address = "123 Fake Street",
                Phone1 = "5555555555",
                UserTypeId = 3, // Resident
                RegistrationNumber = "05298795064",
                User = new UserCreateDTO
                {
                    Email = "unknownrole@example.com",
                    Password = "Aa1!aaaa",
                    Enabled = true
                }
            };

            var result = await PerformForbiddenAddUserTest(fakeUserCreateDTO, "NotARealRole", "not authorized to register");
            Console.WriteLine("AddUserWithUnknownCallerRoleIsForbidden: " + result.Value);
        }

        [TestMethod]
        public async Task GetOwnProfileSuccessWithoutViewCapability()
        {
            Console.WriteLine("GetOwnProfileSuccessWithoutViewCapability begin");

            var userProfileController = LoadUserProfileController("Resident", 3);

            var result = await userProfileController.GetUser(3);

            var okResult = SuccessAssert(result);
            Console.WriteLine("GetOwnProfileSuccessWithoutViewCapability: " + okResult.Value);
        }

        [TestMethod]
        public async Task GetOtherProfileForbiddenWithoutViewCapability()
        {
            Console.WriteLine("GetOtherProfileForbiddenWithoutViewCapability begin");

            var userProfileController = LoadUserProfileController("Resident", 3);

            var result = await userProfileController.GetUser(4);

            var forbiddenResult = ForbiddenAssert(result, "not authorized to view");
            Console.WriteLine("GetOtherProfileForbiddenWithoutViewCapability: " + forbiddenResult!.Value);
        }

        [TestMethod]
        public async Task UpdateOwnNameSuccessWithoutEditCapability()
        {
            Console.WriteLine("UpdateOwnNameSuccessWithoutEditCapability begin");

            var userProfileController = LoadUserProfileController("Resident", 3);

            var result = await userProfileController.UpdateUser(3, new UserProfileUpdateDTO { Name = "Updated Resident Name" });

            var okResult = SuccessAssert(result);
            Console.WriteLine("UpdateOwnNameSuccessWithoutEditCapability: " + okResult.Value);
        }

        [TestMethod]
        public async Task UpdateOwnHousingAssignmentForbiddenWithoutAdminCapability()
        {
            Console.WriteLine("UpdateOwnHousingAssignmentForbiddenWithoutAdminCapability begin");

            var userProfileController = LoadUserProfileController("Resident", 3);

            var result = await userProfileController.UpdateUser(3, new UserProfileUpdateDTO { CondominiumId = 2 });

            var forbiddenResult = ForbiddenAssert(result, "administrator can change housing assignment");
            Console.WriteLine("UpdateOwnHousingAssignmentForbiddenWithoutAdminCapability: " + forbiddenResult!.Value);
        }

        [TestMethod]
        public async Task UpdateOtherProfileForbiddenWithoutEditCapability()
        {
            Console.WriteLine("UpdateOtherProfileForbiddenWithoutEditCapability begin");

            var userProfileController = LoadUserProfileController("Resident", 3);

            var result = await userProfileController.UpdateUser(4, new UserProfileUpdateDTO { Name = "Hijacked Name" });

            var forbiddenResult = ForbiddenAssert(result, "not authorized to edit");
            Console.WriteLine("UpdateOtherProfileForbiddenWithoutEditCapability: " + forbiddenResult!.Value);
        }

        [TestMethod]
        public async Task UpdateUserMovesOccupiedSlotBetweenCondominiumsOnCrossTenantMove()
        {
            Console.WriteLine("UpdateUserMovesOccupiedSlotBetweenCondominiumsOnCrossTenantMove begin");

            var userProfileController = LoadUserProfileController("SystemAdministrator", 1);

            var result = await userProfileController.UpdateUser(3, new UserProfileUpdateDTO { CondominiumId = 3 });

            SuccessAssert(result);

            var originCondo = await _context.Condominiums.FindAsync(1);
            var destinationCondo = await _context.Condominiums.FindAsync(3);

            Assert.AreEqual(5, originCondo!.OccupiedUserSlots);
            Assert.AreEqual(1, destinationCondo!.OccupiedUserSlots);
            Console.WriteLine("UpdateUserMovesOccupiedSlotBetweenCondominiumsOnCrossTenantMove: done");
        }

        [TestMethod]
        public async Task UpdateUserForbiddenWhenMovingIntoCondominiumOutsideCallerAuthority()
        {
            Console.WriteLine("UpdateUserForbiddenWhenMovingIntoCondominiumOutsideCallerAuthority begin");

            var userProfileController = LoadUserProfileController("CondominiumAdministrator", 2); // belongs to Condominium 1

            var result = await userProfileController.UpdateUser(3, new UserProfileUpdateDTO { CondominiumId = 3 });

            var forbiddenResult = ForbiddenAssert(result, "not authorized to move users into this condominium");
            Console.WriteLine("UpdateUserForbiddenWhenMovingIntoCondominiumOutsideCallerAuthority: " + forbiddenResult!.Value);
        }

        [TestMethod]
        public async Task DeleteOwnProfileForbiddenSelfServiceNotAllowed()
        {
            Console.WriteLine("DeleteOwnProfileForbiddenSelfServiceNotAllowed begin");

            var userProfileController = LoadUserProfileController("Resident", 3);

            var result = await userProfileController.Delete(3);

            var forbiddenResult = ForbiddenAssert(result, "not authorized to delete");
            Console.WriteLine("DeleteOwnProfileForbiddenSelfServiceNotAllowed: " + forbiddenResult!.Value);
        }

        [TestMethod]
        public async Task DeleteProfileSuccessWithAdminCapability()
        {
            Console.WriteLine("DeleteProfileSuccessWithAdminCapability begin");

            var userProfileController = LoadUserProfileController("CondominiumAdministrator", 2);

            var result = await userProfileController.Delete(3);

            Assert.IsInstanceOfType(result, typeof(OkResult));

            var condo = await _context.Condominiums.FindAsync(1);
            Assert.AreEqual(5, condo!.OccupiedUserSlots);
            Console.WriteLine("DeleteProfileSuccessWithAdminCapability: done");
        }

        private async Task<long> SeedResidentInOtherCondominiumAsync()
        {
            var otherCondoResident = new UserProfile
            {
                Id = 50,
                Name = "Resident in Condominium C",
                Address = "Other Street",
                Phone1 = "9999999999",
                UserTypeId = 3,
                RegistrationNumber = "99999999999",
                CondominiumId = 3,
                TowerId = 4,
                FloorNumber = 1,
                Apartment = 1
            };

            _context.UserProfiles.Add(otherCondoResident);
            await _context.SaveChangesAsync();

            return otherCondoResident.Id;
        }

        [TestMethod]
        public async Task GetProfileSuccessSameTenantWithAdminCapability()
        {
            Console.WriteLine("GetProfileSuccessSameTenantWithAdminCapability begin");

            var userProfileController = LoadUserProfileController("CondominiumAdministrator", 2);

            var result = await userProfileController.GetUser(3);

            var okResult = SuccessAssert(result);
            Console.WriteLine("GetProfileSuccessSameTenantWithAdminCapability: " + okResult.Value);
        }

        [TestMethod]
        public async Task GetProfileForbiddenAcrossTenants()
        {
            Console.WriteLine("GetProfileForbiddenAcrossTenants begin");

            var otherCondoResidentId = await SeedResidentInOtherCondominiumAsync();
            var userProfileController = LoadUserProfileController("CondominiumAdministrator", 2);

            var result = await userProfileController.GetUser(otherCondoResidentId);

            var forbiddenResult = ForbiddenAssert(result, "not authorized to view");
            Console.WriteLine("GetProfileForbiddenAcrossTenants: " + forbiddenResult!.Value);
        }

        [TestMethod]
        public async Task UpdateProfileForbiddenAcrossTenants()
        {
            Console.WriteLine("UpdateProfileForbiddenAcrossTenants begin");

            var otherCondoResidentId = await SeedResidentInOtherCondominiumAsync();
            var userProfileController = LoadUserProfileController("CondominiumAdministrator", 2);

            var result = await userProfileController.UpdateUser(otherCondoResidentId, new UserProfileUpdateDTO { Name = "Hijacked Across Tenants" });

            var forbiddenResult = ForbiddenAssert(result, "not authorized to edit");
            Console.WriteLine("UpdateProfileForbiddenAcrossTenants: " + forbiddenResult!.Value);
        }

        [TestMethod]
        public async Task DeleteProfileForbiddenAcrossTenants()
        {
            Console.WriteLine("DeleteProfileForbiddenAcrossTenants begin");

            var otherCondoResidentId = await SeedResidentInOtherCondominiumAsync();
            var userProfileController = LoadUserProfileController("CondominiumAdministrator", 2);

            var result = await userProfileController.Delete(otherCondoResidentId);

            var forbiddenResult = ForbiddenAssert(result, "not authorized to delete");
            Console.WriteLine("DeleteProfileForbiddenAcrossTenants: " + forbiddenResult!.Value);
        }

        [TestMethod]
        public async Task AddUserForbiddenWhenTargetCondominiumOutsideCallerScope()
        {
            Console.WriteLine("AddUserForbiddenWhenTargetCondominiumOutsideCallerScope begin");

            var fakeUserCreateDTO = new UserProfileCreateDTO
            {
                Name = "Cross Tenant Resident",
                Address = "123 Fake Street",
                Phone1 = "6666666666",
                UserTypeId = 3, // Resident
                RegistrationNumber = "60117386057",
                CondominiumId = 3, // CondominiumAdministrator (id=2) belongs to Condominium 1, not 3
                TowerId = 4,
                FloorId = 1,
                Apartment = 202,
                ParkingSpaceNumber = 40,
                User = new UserCreateDTO
                {
                    Email = "crosstenant@example.com",
                    Password = "Aa1!aaaa",
                    Enabled = true
                }
            };

            var result = await PerformForbiddenAddUserTest(fakeUserCreateDTO, "CondominiumAdministrator", "not authorized to register users in this condominium", callerId: 2);
            Console.WriteLine("AddUserForbiddenWhenTargetCondominiumOutsideCallerScope: " + result.Value);
        }

        [TestMethod]
        public async Task AddUserFailsWhenAConcurrentRegistrationAlreadyFilledTheCondominium()
        {
            Console.WriteLine("AddUserFailsWhenAConcurrentRegistrationAlreadyFilledTheCondominium begin");

            // Simulates a registration racing another one that has already committed, without needing two
            // real contexts and threads: force the tracked Condominium's known "original" occupancy to a
            // value that no longer matches what is actually stored, the same way EF Core's own change
            // tracker would see it after a concurrent write. The upcoming save must then fail on that
            // mismatch instead of silently letting the condominium exceed MaxUsers.
            var condo = await _context.Condominiums.FindAsync(1);
            _context.Entry(condo!).Property(c => c.OccupiedUserSlots).OriginalValue = condo!.OccupiedUserSlots + 1;

            var fakeUserCreateDTO = new UserProfileCreateDTO
            {
                Name = "Fake User",
                Address = "123 Fake Street",
                Phone1 = "7777777777",
                UserTypeId = 3, // Resident
                RegistrationNumber = "70123456088",
                CondominiumId = 1,
                TowerId = 1,
                FloorId = 1,
                Apartment = 301,
                ParkingSpaceNumber = 50,
                User = new UserCreateDTO
                {
                    Email = "racecondition@example.com",
                    Password = "Aa1!aaaa",
                    Enabled = true
                }
            };

            var result = await PerformUnauthorizedAddUserTest(fakeUserCreateDTO, "The maximum number of users allowed for this condominium has been reached");
            Console.WriteLine("AddUserFailsWhenAConcurrentRegistrationAlreadyFilledTheCondominium: " + result.Value);
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

            var result = await PerformBadRequestAddUserTest(fakeUserCreateDTO, "Invalid CPF/CNPJ");
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

            var result = await PerformBadRequestAddUserTest(fakeUserCreateDTO, "No login was provided");
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

            var result = await PerformBadRequestAddUserTest(fakeUserCreateDTO, "Condominium 10 not found");
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

            var result = await PerformUnauthorizedAddUserTest(fakeUserCreateDTO, "Condominium Condominium B is disabled");
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

            var result3 = await PerformUnauthorizedAddUserTest(fakeUserCreateDTO3, "The maximum number of users allowed for this condominium has been reached");

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

            var result1 = await PerformBadRequestAddUserTest(fakeUserCreateDTO1, "User type 99 not found");

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

            var result1 = await PerformBadRequestAddUserTest(fakeUserCreateDTO1, "Invalid apartment number");

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

            var result1 = await PerformBadRequestAddUserTest(fakeUserCreateDTO1, "Tower 2 not found");

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

            var result1 = await PerformBadRequestAddUserTest(fakeUserCreateDTO1, "Invalid floor number. Tower Tower A1 has 4 floor(s)");

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

            var result2 = await PerformUnauthorizedAddUserTest(fakeUserCreateDTO2, "The specified parking space number is already in use");

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

            var result = await PerformUnauthorizedAddUserTest(fakeUserCreateDTO, "Login adminsystem@smartcondohub.com is already registered");
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

            var result = await PerformUnauthorizedAddUserTest(fakeUserCreateDTO, "CPF 51983777404 is already registered");
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
