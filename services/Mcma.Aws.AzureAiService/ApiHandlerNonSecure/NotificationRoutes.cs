using System;
using System.Net;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Amazon.Lambda.Core;
using Mcma.Api;
using Mcma.Core;
using Mcma.Core.Serialization;
using Amazon.Lambda;
using Amazon.Lambda.Model;
using Mcma.Core.Logging;
using System.Linq;

namespace Mcma.Aws.AzureAiService.ApiHandlerNonSecure
{
    public static class NotificationRoutes
    {
        public static async Task ProcessNotificationAsync(McmaApiRequest request, McmaApiResponse response)
        {
            Logger.Debug(nameof(ProcessNotificationAsync));
            Logger.Debug(request.ToMcmaJson().ToString());
            
            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            var jobAssignmentId = request.StageVariables["PublicUrl"] + request.Path; //"/job-assignments/" + request.PathVariables["id"];

            var jobAssignment = await table.GetAsync<JobAssignment>(jobAssignmentId);

            Logger.Debug("jobAssignment = {0}", jobAssignment);

            if (jobAssignment == null)
            {
                Logger.Debug("jobAssignment not found", jobAssignment);
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.StatusMessage = "No resource found on path '" + request.Path + "'.";
                return;
            }

            var notification = request.QueryStringParameters;
            Logger.Debug("notification = {0}", notification);
            if (notification == null || !notification.Any())
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.StatusMessage = "Missing notification in request Query String";
                return;
            }

            // invoking worker lambda function that will process the notification
            var lambdaClient = new AmazonLambdaClient();
            var invokeRequest = new InvokeRequest
            {
                FunctionName = request.StageVariables["WorkerLambdaFunctionName"],
                InvocationType = "Event",
                LogType = "None",
                Payload =
                    new
                    {
                        action = "ProcessNotification",
                        stageVariables = request.StageVariables,
                        jobAssignmentId,
                        notification
                    }.ToMcmaJson().ToString()
            };

            Logger.Debug("Invoking Lambda: {0}", invokeRequest.ToMcmaJson().ToString());

            await lambdaClient.InvokeAsync(invokeRequest);
        }
    }
}
