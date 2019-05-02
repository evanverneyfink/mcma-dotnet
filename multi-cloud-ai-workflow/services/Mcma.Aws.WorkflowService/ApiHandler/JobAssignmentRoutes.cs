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
using Mcma.Aws.Api;
using Mcma.Aws.DynamoDb;
using Mcma.Aws.Lambda;

namespace Mcma.Aws.WorkflowService.ApiHandler
{
    public static class JobAssignmentRoutes
    {
        private static IWorkerInvoker WorkerInvoker { get; } = new LambdaWorkerInvoker();

        public static async Task ProcessNotificationAsync(McmaApiRequestContext requestContext)
        {
            var table = new DynamoDbTable<JobAssignment>(requestContext.TableName());

            var jobAssignment =
                await table.GetAsync(requestContext.PublicUrl() + "/job-assignments/" + requestContext.Request.PathVariables["id"]);

            if (!requestContext.ResourceIfFound(jobAssignment, false) || requestContext.IsBadRequestDueToMissingBody(out Notification notification))
                return;
                
            await WorkerInvoker.RunAsync(
                requestContext.WorkerFunctionName(),
                new
                {
                    operationName = "ProcessNotification",
                    contextVariables = requestContext.ContextVariables,
                    input = new
                    {
                        jobAssignmentId = jobAssignment.Id,
                        notification = notification
                    }
                });
        }
    }
}
