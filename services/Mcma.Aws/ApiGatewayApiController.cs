using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.APIGatewayEvents;
using Mcma.Core.Serialization;
using Mcma.Api;
using System.IO;

namespace Mcma.Aws
{
    public class ApiGatewayApiController
    {
        static ApiGatewayApiController()
        {
            McmaTypes.Add<S3Locator>();
        }

        private McmaApiController McmaApiController { get; } = new McmaApiController();

        public void AddRoute(string method, string path, Func<McmaApiRequest, McmaApiResponse, Task> handler)
            => McmaApiController.AddRoute(method, path, handler);

        public async Task<APIGatewayProxyResponse> HandleRequestAsync(APIGatewayProxyRequest @event, ILambdaContext context)
        {
            var request = new McmaApiRequest
            {
                Path = @event.Path,
                HttpMethod = @event.HttpMethod,
                Headers = @event.Headers,
                PathVariables = new Dictionary<string, object>(),
                QueryStringParameters = @event.QueryStringParameters ?? new Dictionary<string, string>(),
                StageVariables = @event.StageVariables ?? new Dictionary<string, string>(),
                Body = @event.Body
            };
            
            var response = await McmaApiController.HandleRequestAsync(request);

            return new APIGatewayProxyResponse
            {
                StatusCode = response.StatusCode,
                Headers = response.Headers,
                Body = response.Body
            };
        }
    }
}