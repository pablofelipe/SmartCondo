using Microsoft.EntityFrameworkCore;
using SmartCondoApi.GraphQL.Inputs;
using SmartCondoApi.Models;
using SmartCondoApi.Models.Permissions;
using SmartCondoApi.Services.Vehicle;

namespace SmartCondoApi.Tests.Services
{
    [TestClass]
    public class VehicleServiceTest
    {
        private string _databaseName = null!;
        private SmartCondoContext _context = null!;
        private VehicleService _service = null!;

        [TestInitialize]
        public void Initialize()
        {
            _databaseName = $"vehicleServiceTest_{Guid.NewGuid()}";

            using (var seedContext = CreateContext())
            {
                seedContext.UserTypes.Add(new UserType { Id = 3, Name = "Resident", Description = "Resident" });

                // Tenant 1: actors 2 (CondominiumAdministrator) and 99 (reused across roles) plus owners
                // 10/20/30/31/999. Tenant 2: actor 4 (CondominiumAdministrator) and owner 40 - exists
                // purely to prove Scope now blocks a same-capability admin from a different tenant.
                // Apartment is set uniformly so GetFilteredVehiclesAsync's list-scoping tests can filter by
                // it directly - EF Core's InMemory provider can't translate the EF.Functions.ILike calls the
                // other filter fields use, so equality-based filters are the only ones testable here.
                seedContext.UserProfiles.AddRange(
                    new UserProfile { Id = 2, Name = "Admin Tenant One", Address = "Addr", Phone1 = "0000000002", UserTypeId = 2, RegistrationNumber = "10000000002", CondominiumId = 1 },
                    new UserProfile { Id = 4, Name = "Admin Tenant Two", Address = "Addr", Phone1 = "0000000004", UserTypeId = 2, RegistrationNumber = "10000000004", CondominiumId = 2 },
                    new UserProfile { Id = 10, Name = "Owner Ten", Address = "Addr", Phone1 = "1111111111", UserTypeId = 3, RegistrationNumber = "10000000010", CondominiumId = 1, Apartment = 1 },
                    new UserProfile { Id = 20, Name = "Owner Twenty", Address = "Addr", Phone1 = "2222222222", UserTypeId = 3, RegistrationNumber = "10000000020", CondominiumId = 1, Apartment = 1 },
                    new UserProfile { Id = 30, Name = "Owner Thirty", Address = "Addr", Phone1 = "3333333333", UserTypeId = 3, RegistrationNumber = "10000000030", CondominiumId = 1, Apartment = 1 },
                    new UserProfile { Id = 31, Name = "Owner ThirtyOne", Address = "Addr", Phone1 = "4444444444", UserTypeId = 3, RegistrationNumber = "10000000031", CondominiumId = 1, Apartment = 1 },
                    new UserProfile { Id = 40, Name = "Owner Forty", Address = "Addr", Phone1 = "6666666666", UserTypeId = 3, RegistrationNumber = "10000000040", CondominiumId = 2, Apartment = 1 },
                    new UserProfile { Id = 99, Name = "Actor NineNine", Address = "Addr", Phone1 = "7777777777", UserTypeId = 3, RegistrationNumber = "10000000099", CondominiumId = 1, Apartment = 1 },
                    new UserProfile { Id = 999, Name = "Owner NineNineNine", Address = "Addr", Phone1 = "5555555555", UserTypeId = 3, RegistrationNumber = "10000000999", CondominiumId = 1, Apartment = 1 }
                );

                seedContext.Vehicles.AddRange(
                    new Models.Vehicle { Id = 1, UserId = 10, LicensePlate = "AAA1111", Brand = "Fiat", Model = "Uno", Color = "White", Type = VehicleTypeEnum.Car, Enabled = true },
                    new Models.Vehicle { Id = 2, UserId = 20, LicensePlate = "BBB2222", Brand = "VW", Model = "Gol", Color = "Black", Type = VehicleTypeEnum.Car, Enabled = true },
                    new Models.Vehicle { Id = 3, UserId = 40, LicensePlate = "CCC0004", Brand = "Fiat", Model = "Palio", Color = "Grey", Type = VehicleTypeEnum.Car, Enabled = true }
                );
                seedContext.SaveChanges();
            }

            // A fresh, untracked context, the same way a real request gets a fresh scoped DbContext
            // that never held these entities in its change tracker before the service call.
            _context = CreateContext();
            _service = new VehicleService(_context);
        }

        private SmartCondoContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<SmartCondoContext>()
                .UseInMemoryDatabase(_databaseName)
                .Options;

            return new SmartCondoContext(options);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context?.Dispose();
        }

        [TestMethod]
        public async Task GetVehicleByIdAsync_OwnerWithoutViewCapability_ReturnsVehicle()
        {
            var actor = new AuthenticatedActor(10, "Resident", true, 1, true);
            var vehicle = await _service.GetVehicleByIdAsync(1, actor);
            Assert.IsNotNull(vehicle);
        }

        [TestMethod]
        public async Task GetVehicleByIdAsync_NonOwnerWithoutViewCapability_Throws()
        {
            var actor = new AuthenticatedActor(99, "Resident", true, 1, true);
            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => _service.GetVehicleByIdAsync(1, actor));
        }

        [TestMethod]
        public async Task GetVehicleByIdAsync_NonOwnerWithViewCapability_ReturnsVehicle()
        {
            var actor = new AuthenticatedActor(99, "CondominiumAdministrator", true, 1, true);
            var vehicle = await _service.GetVehicleByIdAsync(1, actor);
            Assert.IsNotNull(vehicle);
        }

        [TestMethod]
        public async Task GetVehicleByIdAsync_AdminFromDifferentTenant_Throws()
        {
            // Actor 4 has CanViewVehicles but belongs to tenant 2; vehicle 1's owner belongs to tenant 1.
            var actor = new AuthenticatedActor(4, "CondominiumAdministrator", true, 2, true);
            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => _service.GetVehicleByIdAsync(1, actor));
        }

        [TestMethod]
        public async Task GetVehicleByIdAsync_NotFound_ReturnsNull()
        {
            var actor = new AuthenticatedActor(10, "Resident", true, 1, true);
            var vehicle = await _service.GetVehicleByIdAsync(999, actor);
            Assert.IsNull(vehicle);
        }

        [TestMethod]
        public async Task GetFilteredVehiclesAsync_WithoutViewCapability_ReturnsOnlyOwnVehicles()
        {
            var actor = new AuthenticatedActor(10, "Resident", true, 1, true);
            var vehicles = (await _service.GetFilteredVehiclesAsync(new VehicleFilterInput(), actor)).ToList();
            Assert.AreEqual(1, vehicles.Count);
            Assert.AreEqual(10, vehicles[0].UserId);
        }

        [TestMethod]
        public async Task GetFilteredVehiclesAsync_WithoutViewCapability_IgnoresFilterRequirement()
        {
            // Resident (id=10) has no vehicle matching this plate, but self-service ignores filters entirely.
            var actor = new AuthenticatedActor(10, "Resident", true, 1, true);
            var filter = new VehicleFilterInput(LicensePlate: "ZZZ9999");

            var vehicles = (await _service.GetFilteredVehiclesAsync(filter, actor)).ToList();

            Assert.AreEqual(1, vehicles.Count);
        }

        [TestMethod]
        public async Task GetFilteredVehiclesAsync_ScopesToActorTenant_ExcludesOtherTenants()
        {
            // Actor 2 has CanViewVehicles and belongs to tenant 1; vehicle 3's owner (40) belongs to tenant 2.
            var actor = new AuthenticatedActor(2, "CondominiumAdministrator", true, 1, true);
            var filter = new VehicleFilterInput(ApartmentNumber: 1);

            var vehicles = (await _service.GetFilteredVehiclesAsync(filter, actor)).ToList();

            Assert.IsTrue(vehicles.All(v => v.UserId != 40));
            Assert.IsTrue(vehicles.Count > 0);
        }

        [TestMethod]
        public async Task GetFilteredVehiclesAsync_UnrestrictedScope_SeesEveryTenant()
        {
            var actor = new AuthenticatedActor(1, "SystemAdministrator", true, null, true);
            var filter = new VehicleFilterInput(ApartmentNumber: 1);

            var vehicles = (await _service.GetFilteredVehiclesAsync(filter, actor)).ToList();

            Assert.IsTrue(vehicles.Any(v => v.UserId == 40));
        }

        [TestMethod]
        public async Task CreateVehicleAsync_ForSelf_Succeeds()
        {
            var actor = new AuthenticatedActor(30, "Resident", true, 1, true);
            var vehicle = new Models.Vehicle { UserId = 30, LicensePlate = "CCC3333", Brand = "Ford", Model = "Ka", Color = "Blue", Type = VehicleTypeEnum.Car, Enabled = true };

            var created = await _service.CreateVehicleAsync(vehicle, actor);

            Assert.IsNotNull(created);
        }

        [TestMethod]
        public async Task CreateVehicleAsync_ForSomeoneElseWithoutCapability_Throws()
        {
            var actor = new AuthenticatedActor(30, "Resident", true, 1, true);
            var vehicle = new Models.Vehicle { UserId = 31, LicensePlate = "DDD4444", Brand = "Ford", Model = "Ka", Color = "Blue", Type = VehicleTypeEnum.Car, Enabled = true };

            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => _service.CreateVehicleAsync(vehicle, actor));
        }

        [TestMethod]
        public async Task CreateVehicleAsync_ForSomeoneElseWithCapability_Succeeds()
        {
            var actor = new AuthenticatedActor(2, "CondominiumAdministrator", true, 1, true);
            var vehicle = new Models.Vehicle { UserId = 31, LicensePlate = "EEE5555", Brand = "Ford", Model = "Ka", Color = "Blue", Type = VehicleTypeEnum.Car, Enabled = true };

            var created = await _service.CreateVehicleAsync(vehicle, actor);

            Assert.IsNotNull(created);
        }

        [TestMethod]
        public async Task CreateVehicleAsync_AdminFromDifferentTenant_Throws()
        {
            // Actor 4 has CanRegisterVehicles but belongs to tenant 2; the target owner (31) belongs to tenant 1.
            var actor = new AuthenticatedActor(4, "CondominiumAdministrator", true, 2, true);
            var vehicle = new Models.Vehicle { UserId = 31, LicensePlate = "FFF6666", Brand = "Ford", Model = "Ka", Color = "Blue", Type = VehicleTypeEnum.Car, Enabled = true };

            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => _service.CreateVehicleAsync(vehicle, actor));
        }

        [TestMethod]
        public async Task UpdateVehicleAsync_OwnerWithoutEditCapability_CannotReassignOwnership()
        {
            var actor = new AuthenticatedActor(10, "Resident", true, 1, true);
            var vehicle = new Models.Vehicle { Id = 1, UserId = 999, LicensePlate = "AAA1111-X", Brand = "Fiat", Model = "Uno", Color = "Red", Type = VehicleTypeEnum.Car, Enabled = true };

            var updated = await _service.UpdateVehicleAsync(vehicle, actor);

            Assert.IsNotNull(updated);
            Assert.AreEqual(10, updated.UserId);
        }

        [TestMethod]
        public async Task UpdateVehicleAsync_NonOwnerWithoutCapability_Throws()
        {
            var actor = new AuthenticatedActor(99, "Resident", true, 1, true);
            var vehicle = new Models.Vehicle { Id = 1, UserId = 10, LicensePlate = "AAA1111-X", Brand = "Fiat", Model = "Uno", Color = "Red", Type = VehicleTypeEnum.Car, Enabled = true };

            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => _service.UpdateVehicleAsync(vehicle, actor));
        }

        [TestMethod]
        public async Task UpdateVehicleAsync_AdminWithCapability_CanReassignOwnership()
        {
            var actor = new AuthenticatedActor(2, "CondominiumAdministrator", true, 1, true);
            var vehicle = new Models.Vehicle { Id = 1, UserId = 999, LicensePlate = "AAA1111-X", Brand = "Fiat", Model = "Uno", Color = "Red", Type = VehicleTypeEnum.Car, Enabled = true };

            var updated = await _service.UpdateVehicleAsync(vehicle, actor);

            Assert.AreEqual(999, updated.UserId);
        }

        [TestMethod]
        public async Task UpdateVehicleAsync_AdminCannotReassignToDifferentTenant_Throws()
        {
            // Actor 2 has authority over vehicle 1's current tenant (1), but the new owner (40) belongs
            // to tenant 2 - reassignment is itself an administrative act on the destination tenant.
            var actor = new AuthenticatedActor(2, "CondominiumAdministrator", true, 1, true);
            var vehicle = new Models.Vehicle { Id = 1, UserId = 40, LicensePlate = "AAA1111-X", Brand = "Fiat", Model = "Uno", Color = "Red", Type = VehicleTypeEnum.Car, Enabled = true };

            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => _service.UpdateVehicleAsync(vehicle, actor));
        }

        [TestMethod]
        public async Task UpdateVehicleAsync_AdminFromDifferentTenant_Throws()
        {
            // Actor 4 has CanEditVehicles but belongs to tenant 2; vehicle 1's owner belongs to tenant 1.
            var actor = new AuthenticatedActor(4, "CondominiumAdministrator", true, 2, true);
            var vehicle = new Models.Vehicle { Id = 1, UserId = 10, LicensePlate = "AAA1111-X", Brand = "Fiat", Model = "Uno", Color = "Red", Type = VehicleTypeEnum.Car, Enabled = true };

            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => _service.UpdateVehicleAsync(vehicle, actor));
        }

        [TestMethod]
        public async Task UpdateVehicleAsync_NotFound_ReturnsNull()
        {
            var actor = new AuthenticatedActor(10, "Resident", true, 1, true);
            var vehicle = new Models.Vehicle { Id = 999, UserId = 10, LicensePlate = "X", Brand = "X", Model = "X", Color = "X", Type = VehicleTypeEnum.Car, Enabled = true };

            var updated = await _service.UpdateVehicleAsync(vehicle, actor);

            Assert.IsNull(updated);
        }

        [TestMethod]
        public async Task UpdateVehicleAsync_CapabilityGrantedIndependentlyOfViewCapability()
        {
            // CleaningManager: CanRegisterUsers=true -> CanEditVehicles=true, but CanViewUsers=false -> CanViewVehicles=false.
            // Edit authority must not depend on view authority.
            var actor = new AuthenticatedActor(99, "CleaningManager", true, 1, true);
            var vehicle = new Models.Vehicle { Id = 2, UserId = 20, LicensePlate = "BBB2222-X", Brand = "VW", Model = "Gol", Color = "Silver", Type = VehicleTypeEnum.Car, Enabled = true };

            var updated = await _service.UpdateVehicleAsync(vehicle, actor);

            Assert.IsNotNull(updated);
        }

        [TestMethod]
        public async Task DeleteVehicleAsync_Owner_Succeeds()
        {
            var actor = new AuthenticatedActor(10, "Resident", true, 1, true);

            var deleted = await _service.DeleteVehicleAsync(1, actor);

            Assert.IsTrue(deleted);
        }

        [TestMethod]
        public async Task DeleteVehicleAsync_NonOwnerWithoutCapability_Throws()
        {
            var actor = new AuthenticatedActor(99, "Resident", true, 1, true);

            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => _service.DeleteVehicleAsync(1, actor));
        }

        [TestMethod]
        public async Task DeleteVehicleAsync_NonOwnerWithCapability_Succeeds()
        {
            var actor = new AuthenticatedActor(2, "CondominiumAdministrator", true, 1, true);

            var deleted = await _service.DeleteVehicleAsync(2, actor);

            Assert.IsTrue(deleted);
        }

        [TestMethod]
        public async Task DeleteVehicleAsync_AdminFromDifferentTenant_Throws()
        {
            // Actor 4 has CanEditVehicles but belongs to tenant 2; vehicle 1's owner belongs to tenant 1.
            var actor = new AuthenticatedActor(4, "CondominiumAdministrator", true, 2, true);
            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => _service.DeleteVehicleAsync(1, actor));
        }

        [TestMethod]
        public async Task DeleteVehicleAsync_NotFound_ReturnsFalse()
        {
            var actor = new AuthenticatedActor(10, "Resident", true, 1, true);

            var deleted = await _service.DeleteVehicleAsync(999, actor);

            Assert.IsFalse(deleted);
        }
    }
}
