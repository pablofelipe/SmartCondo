using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using SmartCondoApi.Services.Lambda;

namespace SmartCondoApi.Functions
{
    public class DisconnectFunction
    {
        private readonly WebSocketFunctions _webSocketFunctions;

        public DisconnectFunction()
        {
            var serviceProvider = LambdaServiceProvider.GetServiceProvider();
            _webSocketFunctions = serviceProvider.GetRequiredService<WebSocketFunctions>();
        }

        public async Task<APIGatewayProxyResponse> FunctionHandler(
            APIGatewayProxyRequest request, ILambdaContext context)
        {
            return await _webSocketFunctions.DisconnectHandler(request, context);
        }
    }
}