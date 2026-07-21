using Microsoft.EntityFrameworkCore;
using SmartCondoApi.Infra;
using SmartCondoApi.Models;
using System.Security.Claims;

namespace SmartCondoApi.Tests.Infra
{
    [TestClass]
    public class AuthenticatedActorResolverTest
    {
        private SmartCondoContext _context = null!;
        private AuthenticatedActorResolver _resolver = null!;

        [TestInitialize]
        public void Initialize()
        {
            var options = new DbContextOptionsBuilder<SmartCondoContext>()
                .UseInMemoryDatabase($"authenticatedActorResolverTest_{Guid.NewGuid()}")
                .Options;

            _context = new SmartCondoContext(options);

            _context.Condominiums.AddRange(
                new Condominium { Id = 1, Name = "Enabled Condo", Address = "Addr", Enabled = true, MaxUsers = 10, TowerCount = 1 },
                new Condominium { Id = 2, Name = "Disabled Condo", Address = "Addr", Enabled = false, MaxUsers = 10, TowerCount = 1 }
            );

            _context.UserProfiles.AddRange(
                new UserProfile { Id = 1, Name = "System Admin", Address = "Addr", Phone1 = "0000000001", RegistrationNumber = "10000000001", UserTypeId = 1 },
                new UserProfile { Id = 2, Name = "Enabled Resident", Address = "Addr", Phone1 = "0000000002", RegistrationNumber = "10000000002", UserTypeId = 3, CondominiumId = 1 },
                new UserProfile { Id = 3, Name = "Disabled Resident", Address = "Addr", Phone1 = "0000000003", RegistrationNumber = "10000000003", UserTypeId = 3, CondominiumId = 1 },
                new UserProfile { Id = 4, Name = "Resident of Disabled Condo", Address = "Addr", Phone1 = "0000000004", RegistrationNumber = "10000000004", UserTypeId = 3, CondominiumId = 2 }
            );

            _context.Users.AddRange(
                new User { Id = 1, UserName = "sysadmin@example.com", Email = "sysadmin@example.com", Enabled = true, EmailConfirmed = true },
                new User { Id = 2, UserName = "enabled@example.com", Email = "enabled@example.com", Enabled = true, EmailConfirmed = true },
                new User { Id = 3, UserName = "disabled@example.com", Email = "disabled@example.com", Enabled = false, EmailConfirmed = true },
                new User { Id = 4, UserName = "disabledcondo@example.com", Email = "disabledcondo@example.com", Enabled = true, EmailConfirmed = true }
            );

            _context.SaveChanges();

            _resolver = new AuthenticatedActorResolver(_context);
        }

        [TestCleanup]
        public void Cleanup()
        {
            _context?.Dispose();
        }

        private static ClaimsPrincipal PrincipalFor(long id, string role)
        {
            return new ClaimsPrincipal(new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, id.ToString()),
                new Claim(ClaimTypes.Role, role)
            ]));
        }

        [TestMethod]
        public async Task ResolveAsync_EnabledUserWithEnabledCondominium_ReturnsLiveActor()
        {
            var actor = await _resolver.ResolveAsync(PrincipalFor(2, "Resident"));

            Assert.AreEqual(2, actor.Id);
            Assert.AreEqual("Resident", actor.Role);
            Assert.IsTrue(actor.Enabled);
            Assert.AreEqual(1, actor.CondominiumId);
            Assert.IsTrue(actor.CondominiumEnabled);
        }

        [TestMethod]
        public async Task ResolveAsync_UserWithNoCondominium_CondominiumEnabledDefaultsTrue()
        {
            var actor = await _resolver.ResolveAsync(PrincipalFor(1, "SystemAdministrator"));

            Assert.IsNull(actor.CondominiumId);
            Assert.IsTrue(actor.CondominiumEnabled);
        }

        [TestMethod]
        public async Task ResolveAsync_DisabledUser_Throws()
        {
            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => _resolver.ResolveAsync(PrincipalFor(3, "Resident")));
        }

        [TestMethod]
        public async Task ResolveAsync_UserInDisabledCondominium_Throws()
        {
            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => _resolver.ResolveAsync(PrincipalFor(4, "Resident")));
        }

        [TestMethod]
        public async Task ResolveAsync_UnknownActor_Throws()
        {
            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => _resolver.ResolveAsync(PrincipalFor(999, "Resident")));
        }

        [TestMethod]
        public async Task ResolveAsync_MissingClaims_Throws()
        {
            var principal = new ClaimsPrincipal(new ClaimsIdentity());

            await Assert.ThrowsExceptionAsync<UnauthorizedAccessException>(() => _resolver.ResolveAsync(principal));
        }
    }
}
