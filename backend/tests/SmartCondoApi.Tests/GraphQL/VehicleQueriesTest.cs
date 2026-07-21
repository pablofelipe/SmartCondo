using HotChocolate;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Moq;
using SmartCondoApi.GraphQL.Inputs;
using SmartCondoApi.GraphQL.Queries;
using SmartCondoApi.Infra;
using SmartCondoApi.Models.Permissions;
using SmartCondoApi.Services.Vehicle;
using System.Security.Claims;

namespace SmartCondoApi.Tests.GraphQL
{
    [TestClass]
    public class VehicleQueriesTest
    {
        private static IHttpContextAccessor HttpContextAccessorFor(long actorId, string role)
        {
            var claims = new ClaimsIdentity([
                new Claim(ClaimTypes.NameIdentifier, actorId.ToString()),
                new Claim(ClaimTypes.Role, role)
            ]);

            var httpContext = new DefaultHttpContext { User = new ClaimsPrincipal(claims) };
            var accessor = new Mock<IHttpContextAccessor>();
            accessor.Setup(a => a.HttpContext).Returns(httpContext);
            return accessor.Object;
        }

        private static IAuthenticatedActorResolver ResolverFor(long actorId, string role)
        {
            var actor = new AuthenticatedActor(actorId, role, true, null, true);
            var resolver = new Mock<IAuthenticatedActorResolver>();
            resolver.Setup(r => r.ResolveAsync(It.IsAny<ClaimsPrincipal>())).ReturnsAsync(actor);
            return resolver.Object;
        }

        [TestMethod]
        public async Task GetVehicle_UnexpectedException_DoesNotLeakExceptionMessage()
        {
            var vehicleService = new Mock<IVehicleService>();
            vehicleService
                .Setup(s => s.GetVehicleByIdAsync(It.IsAny<int>(), It.IsAny<AuthenticatedActor>()))
                .ThrowsAsync(new InvalidOperationException("connection string details that must never reach a client"));

            var queries = new VehicleQueries();

            var ex = await Assert.ThrowsExceptionAsync<GraphQLException>(() =>
                queries.GetVehicle(vehicleService.Object, HttpContextAccessorFor(1, "Resident"), Mock.Of<ILogger<VehicleQueries>>(), ResolverFor(1, "Resident"), "1"));

            var message = ex.Errors[0].Message;
            Assert.IsFalse(message.Contains("connection string"), "The GraphQL error must not leak the underlying exception message");
        }

        [TestMethod]
        public async Task GetVehicle_InvalidId_StillReturnsTheSpecificValidationMessage()
        {
            var vehicleService = new Mock<IVehicleService>();
            var queries = new VehicleQueries();

            var ex = await Assert.ThrowsExceptionAsync<GraphQLException>(() =>
                queries.GetVehicle(vehicleService.Object, HttpContextAccessorFor(1, "Resident"), Mock.Of<ILogger<VehicleQueries>>(), ResolverFor(1, "Resident"), "not-a-number"));

            Assert.AreEqual("VehicleID must be numeric", ex.Errors[0].Message);
        }

        [TestMethod]
        public async Task GetVehicles_UnexpectedException_DoesNotLeakExceptionMessage()
        {
            var vehicleService = new Mock<IVehicleService>();
            vehicleService
                .Setup(s => s.GetFilteredVehiclesAsync(It.IsAny<VehicleFilterInput>(), It.IsAny<AuthenticatedActor>()))
                .ThrowsAsync(new InvalidOperationException("connection string details that must never reach a client"));

            var queries = new VehicleQueries();

            var ex = await Assert.ThrowsExceptionAsync<GraphQLException>(() =>
                queries.GetVehicles(vehicleService.Object, HttpContextAccessorFor(1, "Resident"), Mock.Of<ILogger<VehicleQueries>>(), ResolverFor(1, "Resident")));

            var message = ex.Errors[0].Message;
            Assert.IsFalse(message.Contains("connection string"), "The GraphQL error must not leak the underlying exception message");
        }
    }
}
