using System;
using System.Net;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Mcma.Api;
using Mcma.Aws;
using Mcma.Core;
using Mcma.Core.Serialization;
using Newtonsoft.Json.Linq;
using Amazon.Lambda.APIGatewayEvents;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]

namespace Mcma.Aws.Workflows.WorkflowActivityCallbackHandler
{
    public class Function
    {
        private static ApiGatewayApiController Controller = new ApiGatewayApiController();

        static Function()
        {
            Controller.AddRoute("POST", "/notifications", ProcessNotificationAsync);
        }

        private static async Task ProcessNotificationAsync(McmaApiRequest request, McmaApiResponse response)
        {
            Console.WriteLine(nameof(ProcessNotificationAsync));
            Console.WriteLine(request.ToMcmaJson().ToString());

            dynamic notification = request.JsonBody;

            if (notification == null)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.StatusMessage = "Missing notification in request body";
                return;
            }

            if (notification.content == null)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.StatusMessage = "Missing notification content";
                return;
            }

            if (notification.content.status == null)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.StatusMessage = "Missing notification content status";
                return;
            }

            var stepFunctionClient = new AmazonStepFunctionsClient();
            switch (notification.content.status)
            {
                case "COMPLETED":
                    await stepFunctionClient.SendTaskSuccessAsync(new SendTaskSuccessRequest
                    {
                        TaskToken = request.QueryStringParameters["taskToken"],
                        Output = notification.source.ToMcmaJson()
                    });
                    break;
                case "FAILED":
                    var error = notification.content["@type"] + " failed execution";
                    var cause = notification.content["@type"] + " with id '" + notification.source + "' failed execution with statusMessage '" + notification.content.statusMessage + "'";

                    await stepFunctionClient.SendTaskFailureAsync(new SendTaskFailureRequest
                    {
                        TaskToken = request.QueryStringParameters["taskToken"],
                        Error = error,
                        Cause = cause.ToMcmaJson().ToString()
                    });
                    break;
            }
        }

        public Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            Console.WriteLine(request.ToMcmaJson().ToString());
            Console.WriteLine(context.ToMcmaJson().ToString());

            return Controller.HandleRequestAsync(request, context);
        }
    }
}