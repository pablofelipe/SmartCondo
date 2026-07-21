using Microsoft.AspNetCore.Routing;

namespace SmartCondoApi.GraphQL
{
    public static class GraphQLEndpoints
    {
        public static void Map(IEndpointRouteBuilder endpoints)
        {
            endpoints.MapGraphQL().RequireAuthorization();
        }
    }
}
