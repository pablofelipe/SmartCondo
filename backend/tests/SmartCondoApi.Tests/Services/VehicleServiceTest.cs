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

                seedContext.UserProfiles.AddRange(
                    new UserProfile { Id = 10, Name = "Owner Ten", Address = "Addr", Phone1 = "1111111111", UserTypeId = 3, RegistrationNumber = "10000000010" },
                    new UserProfile { Id = 20, Name = "Owner Twenty", Address = "Addr", Phone1 = "2222222222", UserTypeId = 3, RegistrationNumber = "10000000020" },
                    new UserProfile { Id = 30, Name = "Owner Thirty", Address = "Addr", Phone1 = "3333333333", UserTypeId = 3, RegistrationNumber = "10000000030" },
                    new UserProfile { Id = 31, Name = "Owner ThirtyOne", Address = "Addr", Phone1 = "4444444444", UserTypeId = 3, RegistrationNumber = "10000000031" },
                    new UserProfile { Id = 999, Name = "Owner NineNineNine", Address = "Addr", Phone1 = "5555555555", UserTypeId = 3, RegistrationNumber = "10000000999" }
                );

                seedContext.Vehicles.AddRange(
                    new Models.Vehicle { Id = 1, UserId = 10, LicensePlate = "AAA1111", Brand = "Fiat", Model = "Uno", Color = "White", Type = VehicleTypeEnum.Car, Enabled = true },
                    new Models.Vehicle { Id = 2, UserId = 20, LicensePlate = "BBB2222", Brand = "VW", Model = "Gol", Color = "Black", Type = VehicleTypeEnum.Car, Enabled = true }
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
            var actor = new AuthenticatedActor(10, "Resident");
            var vehicle = await _service.GetVehicleByIdAsync(1, actor);
            Assert.IsNotNull(vehicle);
        }

        [TestMethod]
        public async Task GetVehicleByIdAsync_NonOwnerWithoutViewCapability_Throws()
        {
            var actor = new AuthenticatedActor(99, "Resident");
            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => _service.GetVehicleByIdAsync(1, actor));
        }

        [TestMethod]
        public async Task GetVehicleByIdAsync_NonOwnerWithViewCapability_ReturnsVehicle()
        {
            var actor = new AuthenticatedActor(99, "CondominiumAdministrator");
            var vehicle = await _service.GetVehicleByIdAsync(1, actor);
            Assert.IsNotNull(vehicle);
        }

        [TestMethod]
        public async Task GetVehicleByIdAsync_NotFound_ReturnsNull()
        {
            var actor = new AuthenticatedActor(10, "Resident");
            var vehicle = await _service.GetVehicleByIdAsync(999, actor);
            Assert.IsNull(vehicle);
        }

        [TestMethod]
        public async Task GetFilteredVehiclesAsync_WithoutViewCapability_ReturnsOnlyOwnVehicles()
        {
            var actor = new AuthenticatedActor(10, "Resident");
            var vehicles = (await _service.GetFilteredVehiclesAsync(new VehicleFilterInput(), actor)).ToList();
            Assert.AreEqual(1, vehicles.Count);
            Assert.AreEqual(10, vehicles[0].UserId);
        }

        [TestMethod]
        public async Task GetFilteredVehiclesAsync_WithoutViewCapability_IgnoresFilterRequirement()
        {
            // Resident (id=10) has no vehicle matching this plate, but self-service ignores filters entirely.
            var actor = new AuthenticatedActor(10, "Resident");
            var filter = new VehicleFilterInput(LicensePlate: "ZZZ9999");

            var vehicles = (await _service.GetFilteredVehiclesAsync(filter, actor)).ToList();

            Assert.AreEqual(1, vehicles.Count);
        }

        [TestMethod]
        public async Task CreateVehicleAsync_ForSelf_Succeeds()
        {
            var actor = new AuthenticatedActor(30, "Resident");
            var vehicle = new Models.Vehicle { UserId = 30, LicensePlate = "CCC3333", Brand = "Ford", Model = "Ka", Color = "Blue", Type = VehicleTypeEnum.Car, Enabled = true };

            var created = await _service.CreateVehicleAsync(vehicle, actor);

            Assert.IsNotNull(created);
        }

        [TestMethod]
        public async Task CreateVehicleAsync_ForSomeoneElseWithoutCapability_Throws()
        {
            var actor = new AuthenticatedActor(30, "Resident");
            var vehicle = new Models.Vehicle { UserId = 31, LicensePlate = "DDD4444", Brand = "Ford", Model = "Ka", Color = "Blue", Type = VehicleTypeEnum.Car, Enabled = true };

            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => _service.CreateVehicleAsync(vehicle, actor));
        }

        [TestMethod]
        public async Task CreateVehicleAsync_ForSomeoneElseWithCapability_Succeeds()
        {
            var actor = new AuthenticatedActor(2, "CondominiumAdministrator");
            var vehicle = new Models.Vehicle { UserId = 31, LicensePlate = "EEE5555", Brand = "Ford", Model = "Ka", Color = "Blue", Type = VehicleTypeEnum.Car, Enabled = true };

            var created = await _service.CreateVehicleAsync(vehicle, actor);

            Assert.IsNotNull(created);
        }

        [TestMethod]
        public async Task UpdateVehicleAsync_OwnerWithoutEditCapability_CannotReassignOwnership()
        {
            var actor = new AuthenticatedActor(10, "Resident");
            var vehicle = new Models.Vehicle { Id = 1, UserId = 999, LicensePlate = "AAA1111-X", Brand = "Fiat", Model = "Uno", Color = "Red", Type = VehicleTypeEnum.Car, Enabled = true };

            var updated = await _service.UpdateVehicleAsync(vehicle, actor);

            Assert.IsNotNull(updated);
            Assert.AreEqual(10, updated.UserId);
        }

        [TestMethod]
        public async Task UpdateVehicleAsync_NonOwnerWithoutCapability_Throws()
        {
            var actor = new AuthenticatedActor(99, "Resident");
            var vehicle = new Models.Vehicle { Id = 1, UserId = 10, LicensePlate = "AAA1111-X", Brand = "Fiat", Model = "Uno", Color = "Red", Type = VehicleTypeEnum.Car, Enabled = true };

            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => _service.UpdateVehicleAsync(vehicle, actor));
        }

        [TestMethod]
        public async Task UpdateVehicleAsync_AdminWithCapability_CanReassignOwnership()
        {
            var actor = new AuthenticatedActor(2, "CondominiumAdministrator");
            var vehicle = new Models.Vehicle { Id = 1, UserId = 999, LicensePlate = "AAA1111-X", Brand = "Fiat", Model = "Uno", Color = "Red", Type = VehicleTypeEnum.Car, Enabled = true };

            var updated = await _service.UpdateVehicleAsync(vehicle, actor);

            Assert.AreEqual(999, updated.UserId);
        }

        [TestMethod]
        public async Task UpdateVehicleAsync_NotFound_ReturnsNull()
        {
            var actor = new AuthenticatedActor(10, "Resident");
            var vehicle = new Models.Vehicle { Id = 999, UserId = 10, LicensePlate = "X", Brand = "X", Model = "X", Color = "X", Type = VehicleTypeEnum.Car, Enabled = true };

            var updated = await _service.UpdateVehicleAsync(vehicle, actor);

            Assert.IsNull(updated);
        }

        [TestMethod]
        public async Task UpdateVehicleAsync_CapabilityGrantedIndependentlyOfViewCapability()
        {
            // CleaningManager: CanRegisterUsers=true -> CanEditVehicles=true, but CanViewUsers=false -> CanViewVehicles=false.
            // Edit authority must not depend on view authority.
            var actor = new AuthenticatedActor(99, "CleaningManager");
            var vehicle = new Models.Vehicle { Id = 2, UserId = 20, LicensePlate = "BBB2222-X", Brand = "VW", Model = "Gol", Color = "Silver", Type = VehicleTypeEnum.Car, Enabled = true };

            var updated = await _service.UpdateVehicleAsync(vehicle, actor);

            Assert.IsNotNull(updated);
        }

        [TestMethod]
        public async Task DeleteVehicleAsync_Owner_Succeeds()
        {
            var actor = new AuthenticatedActor(10, "Resident");

            var deleted = await _service.DeleteVehicleAsync(1, actor);

            Assert.IsTrue(deleted);
        }

        [TestMethod]
        public async Task DeleteVehicleAsync_NonOwnerWithoutCapability_Throws()
        {
            var actor = new AuthenticatedActor(99, "Resident");

            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => _service.DeleteVehicleAsync(1, actor));
        }

        [TestMethod]
        public async Task DeleteVehicleAsync_NonOwnerWithCapability_Succeeds()
        {
            var actor = new AuthenticatedActor(2, "CondominiumAdministrator");

            var deleted = await _service.DeleteVehicleAsync(2, actor);

            Assert.IsTrue(deleted);
        }

        [TestMethod]
        public async Task DeleteVehicleAsync_NotFound_ReturnsFalse()
        {
            var actor = new AuthenticatedActor(10, "Resident");

            var deleted = await _service.DeleteVehicleAsync(999, actor);

            Assert.IsFalse(deleted);
        }
    }
}
