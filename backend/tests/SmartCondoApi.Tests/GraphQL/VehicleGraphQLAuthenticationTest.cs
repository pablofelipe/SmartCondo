using System.Net;
using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.TestHost;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using SmartCondoApi.GraphQL;
using SmartCondoApi.GraphQL.Mutations;
using SmartCondoApi.GraphQL.Queries;
using SmartCondoApi.Infra;
using SmartCondoApi.Models;
using SmartCondoApi.Services.Vehicle;
using SmartCondoApi.Tests.Helpers;

namespace SmartCondoApi.Tests.GraphQL
{
    [TestClass]
    public class VehicleGraphQLAuthenticationTest
    {
        private const string VehiclesQuery = "{\"query\":\"{ vehicles { id } }\"}";

        private static async Task<TestServer> BuildServerAsync()
        {
            var host = await new HostBuilder()
                .ConfigureWebHost(webBuilder =>
                {
                    webBuilder.UseTestServer();
                    webBuilder.ConfigureServices(services =>
                    {
                        services.AddDbContext<SmartCondoContext>(options =>
                            options.UseInMemoryDatabase($"graphQLAuthTest_{Guid.NewGuid()}"));
                        services.AddScoped<IVehicleService, VehicleService>();
                        services.AddRouting();

                        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                            .AddJwtBearer(options =>
                            {
                                options.TokenValidationParameters = new TokenValidationParameters
                                {
                                    ValidateIssuer = true,
                                    ValidateAudience = true,
                                    ValidateLifetime = true,
                                    ValidateIssuerSigningKey = true,
                                    ValidIssuer = "IssuerTest",
                                    ValidAudience = "AudienceTest",
                                    IssuerSigningKey = new SymmetricSecurityKey(
                                        Convert.FromBase64String("QUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUFBQUE="))
                                };
                            });
                        services.AddAuthorization();

                        services.AddGraphQLServer()
                            .AddQueryType<Query>()
                            .AddMutationType<Mutation>()
                            .AddTypeExtension<VehicleQueries>()
                            .AddTypeExtension<VehicleMutations>()
                            .AddVehicleTypes()
                            .AddProjections();
                    });
                    webBuilder.Configure(app =>
                    {
                        app.UseRouting();
                        app.UseAuthentication();
                        app.UseAuthorization();
                        app.UseEndpoints(endpoints =>
                        {
                            SmartCondoApi.GraphQL.GraphQLEndpoints.Map(endpoints);
                        });
                    });
                })
                .StartAsync();

            return host.GetTestServer();
        }

        private static string BuildValidToken()
        {
            var configuration = TestHelper.CreateConfiguration();
            var tokenHandler = new SmartCondoApi.Infra.TokenHandler(configuration);

            return tokenHandler.Generate("1", "resident@example.com", "Resident", DateTime.UtcNow.AddMinutes(5));
        }

        [TestMethod]
        public async Task VehicleQueryWithoutAuthenticationIsRejected()
        {
            var server = await BuildServerAsync();
            var client = server.CreateClient();

            var response = await client.PostAsync("/graphql",
                new StringContent(VehiclesQuery, Encoding.UTF8, "application/json"));

            Assert.AreEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }

        [TestMethod]
        public async Task VehicleQueryWithAuthenticationPassesTheAuthorizationGate()
        {
            var server = await BuildServerAsync();
            var client = server.CreateClient();
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", BuildValidToken());

            var response = await client.PostAsync("/graphql",
                new StringContent(VehiclesQuery, Encoding.UTF8, "application/json"));

            Assert.AreNotEqual(HttpStatusCode.Unauthorized, response.StatusCode);
        }
    }
}
