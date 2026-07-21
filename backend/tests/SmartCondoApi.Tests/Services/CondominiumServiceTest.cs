using Microsoft.EntityFrameworkCore;
using SmartCondoApi.Dto;
using SmartCondoApi.Exceptions;
using SmartCondoApi.Models;
using SmartCondoApi.Models.Permissions;
using SmartCondoApi.Services.Condominium;

namespace SmartCondoApi.Tests.Services
{
    [TestClass]
    public class CondominiumServiceTest
    {
        private string _databaseName = null!;
        private SmartCondoContext _context = null!;
        private CondominiumService _service = null!;

        [TestInitialize]
        public void Initialize()
        {
            _databaseName = $"condominiumServiceTest_{Guid.NewGuid()}";

            using (var seedContext = CreateContext())
            {
                seedContext.UserTypes.Add(new UserType { Id = 3, Name = "Resident", Description = "Resident" });

                seedContext.Condominiums.AddRange(
                    new Models.Condominium { Id = 1, Name = "Condominium A", Address = "Addr A", Enabled = true, MaxUsers = 10, TowerCount = 1 },
                    new Models.Condominium { Id = 2, Name = "Condominium B", Address = "Addr B", Enabled = true, MaxUsers = 10, TowerCount = 1 },
                    new Models.Condominium { Id = 3, Name = "Condominium C (empty)", Address = "Addr C", Enabled = true, MaxUsers = 10, TowerCount = 0 }
                );

                // Actor profiles - carry no capability of their own; RolePermissions.GetPermissions()
                // is looked up from the AuthenticatedActor's Role claim, not from these rows. Only
                // CondominiumId matters here, to represent "which tenant this actor belongs to".
                seedContext.UserProfiles.AddRange(
                    new UserProfile { Id = 10, Name = "Actor in A", Address = "Addr", Phone1 = "1111111111", UserTypeId = 3, RegistrationNumber = "10000000001", CondominiumId = 1 },
                    new UserProfile { Id = 20, Name = "Actor in B", Address = "Addr", Phone1 = "2222222222", UserTypeId = 3, RegistrationNumber = "10000000002", CondominiumId = 2 }
                );

                seedContext.SaveChanges();
            }

            _context = CreateContext();
            _service = new CondominiumService(_context);
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
        public async Task Get_PlatformOperator_ReturnsAllCondominiums()
        {
            var actor = new AuthenticatedActor(10, "SystemAdministrator");

            var condos = (await _service.Get(actor)).ToList();

            Assert.AreEqual(3, condos.Count);
        }

        [TestMethod]
        public async Task Get_NonCapableRole_ReturnsEmpty()
        {
            // CondominiumAdministrator has no Condominium capability today - Scope restricts, never grants,
            // so even for their own tenant the list stays empty. See ADR-0005.
            var actor = new AuthenticatedActor(10, "CondominiumAdministrator");

            var condos = (await _service.Get(actor)).ToList();

            Assert.AreEqual(0, condos.Count);
        }

        [TestMethod]
        public async Task GetById_PlatformOperator_SucceedsForAnyTenant()
        {
            var actor = new AuthenticatedActor(10, "SystemAdministrator");

            var condo = await _service.Get(2, actor);

            Assert.IsNotNull(condo);
        }

        [TestMethod]
        public async Task GetById_NonCapableRoleOwnTenant_Denied()
        {
            var actor = new AuthenticatedActor(10, "CondominiumAdministrator"); // belongs to Condominium 1

            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => _service.Get(1, actor));
        }

        [TestMethod]
        public async Task GetById_NonCapableRoleOtherTenant_Denied()
        {
            var actor = new AuthenticatedActor(10, "CondominiumAdministrator"); // belongs to Condominium 1

            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => _service.Get(2, actor));
        }

        [TestMethod]
        public async Task GetById_NotFound_Throws()
        {
            var actor = new AuthenticatedActor(10, "SystemAdministrator");

            await Assert.ThrowsExceptionAsync<CondominiumNotFoundException>(() => _service.Get(999, actor));
        }

        [TestMethod]
        public async Task Create_PlatformOperator_Succeeds()
        {
            var actor = new AuthenticatedActor(10, "SystemAdministrator");
            var dto = new CondominiumCreateDTO { Name = "New Condo", Address = "New Addr", TowerCount = 1, MaxUsers = 5, Enabled = true };

            var created = await _service.Create(dto, actor);

            Assert.IsNotNull(created);
        }

        [TestMethod]
        public async Task Create_NonCapableRole_Denied()
        {
            var actor = new AuthenticatedActor(10, "CondominiumAdministrator");
            var dto = new CondominiumCreateDTO { Name = "New Condo", Address = "New Addr", TowerCount = 1, MaxUsers = 5, Enabled = true };

            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => _service.Create(dto, actor));
        }

        [TestMethod]
        public async Task Update_PlatformOperator_Succeeds()
        {
            var actor = new AuthenticatedActor(10, "SystemAdministrator");

            await _service.Update(1, new CondominiumUpdateDTO { Name = "Renamed" }, actor);

            var updated = await _context.Condominiums.FindAsync(1);
            Assert.AreEqual("Renamed", updated!.Name);
        }

        [TestMethod]
        public async Task Update_NonCapableRoleOwnTenant_Denied()
        {
            var actor = new AuthenticatedActor(10, "CondominiumAdministrator");

            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => _service.Update(1, new CondominiumUpdateDTO { Name = "Renamed" }, actor));
        }

        [TestMethod]
        public async Task Delete_PlatformOperator_HardDeletesWhenNoUsers()
        {
            var actor = new AuthenticatedActor(10, "SystemAdministrator");

            await _service.Delete(3, actor);

            var deleted = await _context.Condominiums.FindAsync(3);
            Assert.IsNull(deleted);
        }

        [TestMethod]
        public async Task Delete_PlatformOperator_SoftDeletesWhenUsersExist()
        {
            var actor = new AuthenticatedActor(10, "SystemAdministrator");

            await _service.Delete(2, actor); // has a seeded actor profile

            var condo = await _context.Condominiums.FindAsync(2);
            Assert.IsNotNull(condo);
            Assert.IsFalse(condo!.Enabled);
        }

        [TestMethod]
        public async Task Delete_NonCapableRole_Denied()
        {
            var actor = new AuthenticatedActor(10, "CondominiumAdministrator");

            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => _service.Delete(1, actor));
        }

        [TestMethod]
        public async Task SearchUsers_CapableRoleOwnTenant_Succeeds()
        {
            // CondominiumAdministrator has CanViewUsers=true - this is the non-degenerate Scope case,
            // unlike the rest of this controller which only SystemAdministrator can reach today.
            var actor = new AuthenticatedActor(10, "CondominiumAdministrator"); // belongs to Condominium 1

            var users = await _service.SearchUsers(1, new UserProfileSearchDTO { Name = "Actor" }, actor);

            Assert.IsNotNull(users);
        }

        [TestMethod]
        public async Task SearchUsers_CapableRoleOtherTenant_Denied()
        {
            var actor = new AuthenticatedActor(10, "CondominiumAdministrator"); // belongs to Condominium 1

            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(
                () => _service.SearchUsers(2, new UserProfileSearchDTO { Name = "Actor" }, actor));
        }
    }
}
