using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Moq;
using SmartCondoApi.Models;

namespace SmartCondoApi.Tests.Helpers
{
    public static class TestHelper
    {
        private static Dictionary<string, string> userSecrets = new()
            {
                { "adminsystem@smartcondohub.com", "Aa@123!" },
                { "adminA@aaa.com", "Bb@321!" },
                { "resident001@aaa.com", "Cc@456!" },
                { "resident002@aaa.com", "Cc@466!" },
                { "resident003@aaa.com", "Dd6266!" },
                { "resident004@aaa.com", "Xx45%123" },
                { "unknowUserType01@aaa.com", "V6!]Bq3;" },
                { "resident005@aaa.com", "W8bnw#" },
            };


        public static SmartCondoContext SeedTestData()
        {
            var options = new DbContextOptionsBuilder<SmartCondoContext>()
                .UseInMemoryDatabase(databaseName: $"smartCondoTestDb_{Guid.NewGuid()}")
                .Options;

            var context = new SmartCondoContext(options);

            var condominiumA = new Condominium()
            {
                Id = 1,
                Name = "Condominium A",
                Address = "Aaa Avenue, 123",
                Enabled = true,
                MaxUsers = 10,
                TowerCount = 2,
            };

            // Matches the six UserProfiles seeded below with CondominiumId = 1 (ids 2, 3, 4, 6, 7, 8) -
            // OccupiedUserSlots must reflect real occupancy, the same way the production migration
            // backfills it, or the quota check would think this condominium has more room than it does.
            for (var i = 0; i < 6; i++)
            {
                condominiumA.TryOccupyUserSlot();
            }

            context.Condominiums.AddRange(
                condominiumA,
                new Condominium()
                {
                    Id = 2,
                    Name = "Condominium B",
                    Address = "Bbb Avenue, 456",
                    Enabled = false,
                    MaxUsers = 5,
                    TowerCount = 1,
                },
                new Condominium()
                {
                    Id = 3,
                    Name = "Condominium C",
                    Address = "Ccc Avenue, 789",
                    Enabled = true,
                    MaxUsers = 2,
                    TowerCount = 1,
                }
                );

            context.Towers.AddRange(
                new Tower()
                {
                    Id = 1,
                    CondominiumId = 1,
                    FloorCount = 4,
                    Number = 1,
                    Name = "Tower A1"
                },
                new Tower()
                {
                    Id = 2,
                    CondominiumId = 1,
                    FloorCount = 4,
                    Number = 2,
                    Name = "Tower A2"
                },
                new Tower()
                {
                    Id = 3,
                    CondominiumId = 2,
                    FloorCount = 2,
                    Number = 1,
                    Name = "Tower B"
                },
                new Tower()
                {
                    Id = 4,
                    CondominiumId = 3,
                    FloorCount = 2,
                    Number = 1,
                    Name = "Tower C"
                }
                );

            context.UserTypes.AddRange(
                new UserType()
                {
                    Id = 1,
                    Name = "SystemAdministrator",
                    Description = "Administrator of System"
                },
                new UserType()
                {
                    Id = 2,
                    Name = "CondominiumAdministrator",
                    Description = "Administrator of Condominium"
                },
                new UserType()
                {
                    Id = 3,
                    Name = "Resident",
                    Description = "Resident of Condominium"
                }
                );

            context.UserProfiles.AddRange(
                new UserProfile
                {
                    Id = 1,
                    UserTypeId = 1,
                    Name = "Admin smartCondo",
                    Address = "Wow Avenue, 321",
                    Phone1 = "9985439568",
                    RegistrationNumber = "95376095886",
                },
                new UserProfile
                {
                    Id = 2,
                    UserTypeId = 2,
                    Name = "Admin Condominium A",
                    Address = "Aaa Avenue, 111",
                    Phone1 = "9985469851",
                    RegistrationNumber = "51983777404",
                    CondominiumId = 1,
                },
                new UserProfile
                {
                    Id = 3,
                    UserTypeId = 3,
                    Name = "Resident 1 on A",
                    Address = "Aaa Avenue, 111",
                    Phone1 = "9945893125",
                    RegistrationNumber = "80385312059",
                    CondominiumId = 1,
                    TowerId = 1,
                    FloorNumber = 1,
                    Apartment = 1
                },
                new UserProfile
                {
                    Id = 4,
                    UserTypeId = 4,
                    Name = "Resident 2 on A",
                    Address = "Aaa Avenue, 111",
                    Phone1 = "6730196454",
                    RegistrationNumber = "44870635402",
                    CondominiumId = 1,
                    TowerId = 1,
                    FloorNumber = 1,
                    Apartment = 2
                },
                /*user 5*/
                new UserProfile
                {
                    Id = 6,
                    UserTypeId = 4,
                    Name = "Resident 4 on A",
                    Address = "Aaa Avenue, 111",
                    Phone1 = "8726856581",
                    RegistrationNumber = "98191797852",
                    CondominiumId = 1,
                    TowerId = 1,
                    FloorNumber = 1,
                    Apartment = 4
                },
                new UserProfile
                {
                    Id = 7,
                    UserTypeId = 99,
                    Name = "Unkwnown User 01 on A",
                    Address = "Aaa Avenue, 111",
                    Phone1 = "8936731579",
                    RegistrationNumber = "52432809254",
                    CondominiumId = 1,
                    TowerId = 1,
                    FloorNumber = 1,
                    Apartment = 4
                },
                new UserProfile
                {
                    Id = 8,
                    UserTypeId = 3,
                    Name = "Resident 8 on A",
                    Address = "Aaa Avenue, 111",
                    Phone1 = "6720447517",
                    RegistrationNumber = "20609107658",
                    CondominiumId = 1,
                    TowerId = 1,
                    FloorNumber = 1,
                    Apartment = 5
                }
                );

            context.SaveChanges();

            return context;
        }

        public static IConfiguration CreateConfiguration()
        {
            var inMemorySettings = new Dictionary<string, string>
            {
                // Token generation requires a base64 value that decodes to at least 32 bytes
                { "Jwt:Key", "QUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUE=" },
                { "Jwt:Issuer", "IssuerTest" },
                { "Jwt:Audience", "AudienceTest" },
            };

            return new ConfigurationBuilder()
                .AddInMemoryCollection(inMemorySettings)
                .Build();
        }

        public static List<User> GetUsers()
        {
            List<User> users = new();

            users.AddRange(new List<User>
            {
                    new User()
                    {
                        Id = 1,
                        Email = "adminsystem@smartcondohub.com",
                        Enabled = true,
                        EmailConfirmed = true,
                    },
                    new User()
                    {
                        Id = 2,
                        Email = "adminA@aaa.com",
                        Enabled = true,
                        EmailConfirmed = true,
                    },
                    new User()
                    {
                        Id = 3,
                        Email = "resident001@aaa.com",
                        Enabled = true,
                        EmailConfirmed = true,
                    },
                    new User()
                    {
                        Id = 4,
                        Email = "resident002@aaa.com",
                        Enabled = false,
                        EmailConfirmed = true,
                    },
                    new User()
                    {
                        Id = 5,
                        Email = "resident003@aaa.com",
                        Enabled = true,
                        EmailConfirmed = true,
                    },
                    new User()
                    {
                        Id = 6,
                        Email = "resident004@aaa.com",
                        Enabled = true,
                        EmailConfirmed = true,
                    },
                    new User()
                    {
                        Id = 7,
                        Email = "unknowUserType01@aaa.com",
                        Enabled = true,
                        EmailConfirmed = true,
                    },
                    new User()
                    {
                        Id = 8,
                        Email = "resident005@aaa.com",
                        Enabled = true,
                        EmailConfirmed = false,
                    }
                    }
                );


            foreach (var user in users)
            {
                user.UserName = user.Email;
            }

            return users;
        }
        public static async Task<UserManager<User>> CreateUserManagerAsync(SmartCondoContext context)
        {
            UserManager<User> userManager;

            var userStore = new UserStore<User, IdentityRole<long>, SmartCondoContext, long>(context);
            var identityOptions = new IdentityOptions();
            var optionsMock = new Mock<IOptions<IdentityOptions>>();
            optionsMock.Setup(o => o.Value).Returns(identityOptions);

            var passwordHasher = new PasswordHasher<User>();
            var userValidators = new List<IUserValidator<User>> { new UserValidator<User>() };
            var passwordValidators = new List<IPasswordValidator<User>> { new PasswordValidator<User>() };

            var dataProtectionProvider = DataProtectionProvider.Create("SmartCondoApi.Tests");

            var tokenProviderOptions = Options.Create(new DataProtectionTokenProviderOptions
            {
                Name = "Default",
                TokenLifespan = TimeSpan.FromHours(12) // 12 hours
            });

            var logger = new Mock<ILogger<DataProtectorTokenProvider<User>>>().Object;

            // Cria o provedor de token
            var tokenProvider = new DataProtectorTokenProvider<User>(
                dataProtectionProvider,
                tokenProviderOptions,
                logger
            );

            userManager = new UserManager<User>(
                userStore,
                optionsMock.Object,
                passwordHasher,
                userValidators,
                passwordValidators,
                new UpperInvariantLookupNormalizer(),
                new IdentityErrorDescriber(),
                new ServiceCollection().BuildServiceProvider(),
                new Mock<ILogger<UserManager<User>>>().Object
            );

            userManager.RegisterTokenProvider("Default", tokenProvider);

            var users = GetUsers();

            foreach (var usr in users)
            {
                usr.UserName = usr.Email;

                if (!userSecrets.TryGetValue(usr.Email, out var secret))
                {
                    throw new Exception($"Password não encontrado para {usr.Email}");
                }

                var result = await userManager.CreateAsync(usr, secret);

                if (!result.Succeeded)
                {
                    throw new Exception("Falha ao criar o usuário: " + string.Join(", ", result.Errors));
                }
            }

            return userManager;
        }
    }
}
