using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using SmartCondoApi.Services.Lambda;

namespace SmartCondoApi.Functions
{
    public class ConnectFunction
    {
        private readonly WebSocketFunctions _webSocketFunctions;

        public ConnectFunction()
        {
            var serviceProvider = LambdaServiceProvider.GetServiceProvider();
            _webSocketFunctions = serviceProvider.GetRequiredService<WebSocketFunctions>();
        }

        public async Task<APIGatewayProxyResponse> FunctionHandler(
            APIGatewayProxyRequest request, ILambdaContext context)
        {
            return await _webSocketFunctions.ConnectHandler(request, context);
        }
    }
}