using Microsoft.EntityFrameworkCore;
using SmartCondoApi.Models;
using SmartCondoApi.Services.Notification;

namespace SmartCondoApi.Tests.Services
{
    [TestClass]
    public class MessageRecipientResolverTest
    {
        private static SmartCondoContext CreateContext()
        {
            var options = new DbContextOptionsBuilder<SmartCondoContext>()
                .UseInMemoryDatabase($"messageRecipientResolverTest_{Guid.NewGuid()}")
                .Options;

            return new SmartCondoContext(options);
        }

        private static UserProfile MakeProfile(long id, int condominiumId, int? towerId = null, int? floorNumber = null)
        {
            return new UserProfile
            {
                Id = id,
                Name = "Test",
                Address = "Test",
                Phone1 = "0000",
                RegistrationNumber = "000",
                CondominiumId = condominiumId,
                TowerId = towerId,
                FloorNumber = floorNumber
            };
        }

        [TestMethod]
        public async Task ResolveUserIdsAsync_CondominiumScope_ReturnsEveryoneInTheCondominium()
        {
            using var context = CreateContext();
            context.UserProfiles.AddRange(
                MakeProfile(1, condominiumId: 1, towerId: 1),
                MakeProfile(2, condominiumId: 1, towerId: 2),
                MakeProfile(3, condominiumId: 2, towerId: 1));
            await context.SaveChangesAsync();

            var message = new Message { Scope = MessageScope.Condominium, CondominiumId = 1 };

            var userIds = await MessageRecipientResolver.ResolveUserIdsAsync(context, message);

            CollectionAssert.AreEquivalent(new List<long> { 1, 2 }, userIds);
        }

        [TestMethod]
        public async Task ResolveUserIdsAsync_TowerScope_ReturnsOnlyThatTower()
        {
            using var context = CreateContext();
            context.UserProfiles.AddRange(
                MakeProfile(1, condominiumId: 1, towerId: 1),
                MakeProfile(2, condominiumId: 1, towerId: 2));
            await context.SaveChangesAsync();

            var message = new Message { Scope = MessageScope.Tower, CondominiumId = 1, TowerId = 1 };

            var userIds = await MessageRecipientResolver.ResolveUserIdsAsync(context, message);

            CollectionAssert.AreEquivalent(new List<long> { 1 }, userIds);
        }

        [TestMethod]
        public async Task ResolveUserIdsAsync_FloorScope_ReturnsOnlyThatFloor()
        {
            using var context = CreateContext();
            context.UserProfiles.AddRange(
                MakeProfile(1, condominiumId: 1, towerId: 1, floorNumber: 3),
                MakeProfile(2, condominiumId: 1, towerId: 1, floorNumber: 4));
            await context.SaveChangesAsync();

            var message = new Message { Scope = MessageScope.Floor, CondominiumId = 1, TowerId = 1, FloorId = 3 };

            var userIds = await MessageRecipientResolver.ResolveUserIdsAsync(context, message);

            CollectionAssert.AreEquivalent(new List<long> { 1 }, userIds);
        }

        [TestMethod]
        public async Task ResolveUserIdsAsync_IndividualScope_ReturnsOnlyTheRecipient()
        {
            using var context = CreateContext();

            var message = new Message { Scope = MessageScope.Individual, RecipientUserId = 42 };

            var userIds = await MessageRecipientResolver.ResolveUserIdsAsync(context, message);

            CollectionAssert.AreEquivalent(new List<long> { 42 }, userIds);
        }

        [TestMethod]
        public async Task ResolveUserIdsAsync_IndividualScopeWithNoRecipient_ReturnsEmpty()
        {
            using var context = CreateContext();

            var message = new Message { Scope = MessageScope.Individual, RecipientUserId = null };

            var userIds = await MessageRecipientResolver.ResolveUserIdsAsync(context, message);

            Assert.AreEqual(0, userIds.Count);
        }
    }
}
