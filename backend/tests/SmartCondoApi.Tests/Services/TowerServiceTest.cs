using Microsoft.EntityFrameworkCore;
using SmartCondoApi.Dto;
using SmartCondoApi.Exceptions;
using SmartCondoApi.Models;
using SmartCondoApi.Models.Permissions;
using SmartCondoApi.Services.Condominium;

namespace SmartCondoApi.Tests.Services
{
    [TestClass]
    public class TowerServiceTest
    {
        private string _databaseName = null!;
        private SmartCondoContext _context = null!;
        private TowerService _service = null!;

        [TestInitialize]
        public void Initialize()
        {
            _databaseName = $"towerServiceTest_{Guid.NewGuid()}";

            using (var seedContext = CreateContext())
            {
                seedContext.UserTypes.Add(new UserType { Id = 3, Name = "Resident", Description = "Resident" });

                seedContext.Condominiums.AddRange(
                    new Models.Condominium { Id = 1, Name = "Condominium A", Address = "Addr A", Enabled = true, MaxUsers = 10, TowerCount = 1 },
                    new Models.Condominium { Id = 2, Name = "Condominium B", Address = "Addr B", Enabled = true, MaxUsers = 10, TowerCount = 1 }
                );

                seedContext.Towers.AddRange(
                    new Tower { Id = 1, Number = 1, Name = "Tower A1", CondominiumId = 1, FloorCount = 4 },
                    new Tower { Id = 2, Number = 1, Name = "Tower B1", CondominiumId = 2, FloorCount = 4 }
                );

                seedContext.UserProfiles.AddRange(
                    new UserProfile { Id = 10, Name = "Actor in A", Address = "Addr", Phone1 = "1111111111", UserTypeId = 3, RegistrationNumber = "10000000001", CondominiumId = 1 },
                    new UserProfile { Id = 20, Name = "Actor in B", Address = "Addr", Phone1 = "2222222222", UserTypeId = 3, RegistrationNumber = "10000000002", CondominiumId = 2 }
                );

                seedContext.SaveChanges();
            }

            _context = CreateContext();
            _service = new TowerService(_context);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context?.Dispose();
        }

        private SmartCondoContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<SmartCondoContext>()
                .UseInMemoryDatabase(_databaseName)
                .Options;

            return new SmartCondoContext(options);
        }

        [TestMethod]
        public async Task Get_PlatformOperator_SucceedsForAnyTenant()
        {
            var actor = new AuthenticatedActor(10, "SystemAdministrator", true, 1, true);

            var tower = await _service.Get(2, actor);

            Assert.IsNotNull(tower);
        }

        [TestMethod]
        public async Task Get_NonCapableRoleOwnTenant_Denied()
        {
            // Tower has no capability of its own - it derives from Condominium capability,
            // which CondominiumAdministrator does not hold today (same reasoning as CondominiumServiceTest).
            var actor = new AuthenticatedActor(10, "CondominiumAdministrator", true, 1, true); // belongs to Condominium 1, same as Tower 1

            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => _service.Get(1, actor));
        }

        [TestMethod]
        public async Task Get_NonCapableRoleOtherTenant_Denied()
        {
            var actor = new AuthenticatedActor(10, "CondominiumAdministrator", true, 1, true); // belongs to Condominium 1

            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => _service.Get(2, actor));
        }

        [TestMethod]
        public async Task Get_NotFound_Throws()
        {
            var actor = new AuthenticatedActor(10, "SystemAdministrator", true, 1, true);

            await Assert.ThrowsExceptionAsync<TowerNotFoundException>(() => _service.Get(999, actor));
        }

        [TestMethod]
        public async Task GetByCondominium_PlatformOperator_Succeeds()
        {
            var actor = new AuthenticatedActor(10, "SystemAdministrator", true, 1, true);

            var towers = await _service.GetByCondominium(2, actor);

            Assert.AreEqual(1, towers.Count);
        }

        [TestMethod]
        public async Task GetByCondominium_NonCapableRoleOtherTenant_Denied()
        {
            var actor = new AuthenticatedActor(10, "CondominiumAdministrator", true, 1, true); // belongs to Condominium 1

            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => _service.GetByCondominium(2, actor));
        }

        [TestMethod]
        public async Task Create_PlatformOperator_Succeeds()
        {
            var actor = new AuthenticatedActor(10, "SystemAdministrator", true, 1, true);
            var dto = new TowerCreateDTO { Number = 2, Name = "Tower A2", CondominiumId = 1, FloorCount = 3 };

            var created = await _service.Create(dto, actor);

            Assert.IsNotNull(created);
        }

        [TestMethod]
        public async Task Create_NonCapableRole_Denied()
        {
            var actor = new AuthenticatedActor(10, "CondominiumAdministrator", true, 1, true);
            var dto = new TowerCreateDTO { Number = 2, Name = "Tower A2", CondominiumId = 1, FloorCount = 3 };

            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => _service.Create(dto, actor));
        }

        [TestMethod]
        public async Task Update_PlatformOperator_Succeeds()
        {
            var actor = new AuthenticatedActor(10, "SystemAdministrator", true, 1, true);

            await _service.Update(1, new TowerUpdateDTO { Name = "Renamed Tower" }, actor);

            var updated = await _context.Towers.FindAsync(1);
            Assert.AreEqual("Renamed Tower", updated!.Name);
        }

        [TestMethod]
        public async Task Update_NonCapableRoleOwnTenant_Denied()
        {
            var actor = new AuthenticatedActor(10, "CondominiumAdministrator", true, 1, true);

            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => _service.Update(1, new TowerUpdateDTO { Name = "Renamed Tower" }, actor));
        }

        [TestMethod]
        public async Task Delete_PlatformOperator_Succeeds()
        {
            var actor = new AuthenticatedActor(10, "SystemAdministrator", true, 1, true);

            await _service.Delete(2, actor);

            var deleted = await _context.Towers.FindAsync(2);
            Assert.IsNull(deleted);
        }

        [TestMethod]
        public async Task Delete_NonCapableRole_Denied()
        {
            var actor = new AuthenticatedActor(10, "CondominiumAdministrator", true, 1, true);

            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => _service.Delete(1, actor));
        }
    }
}
