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

namespace Mcma.Aws.AmeService.ApiHandler
{
    public static class JobAssignmentRoutes
    {
        public static async Task GetJobAssignmentsAsync(McmaApiRequest request, McmaApiResponse response)
        {
            Logger.Debug(nameof(GetJobAssignmentsAsync));
            Logger.Debug(request.ToMcmaJson().ToString());
            
            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            response.JsonBody = (await table.GetAllAsync<JobAssignment>()).ToMcmaJson();

            Logger.Debug(response.ToMcmaJson().ToString());
        }
        
        public static async Task DeleteJobAssignmentsAsync(McmaApiRequest request, McmaApiResponse response)
        {
            Logger.Debug(nameof(DeleteJobAssignmentsAsync));
            Logger.Debug(request.ToMcmaJson().ToString());

            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            var jobAssignments = await table.GetAllAsync<JobAssignment>();

            foreach (var jobAssignment in jobAssignments)
                await table.DeleteAsync<JobAssignment>(jobAssignment.Id);

            Logger.Debug(response.ToMcmaJson().ToString());
        }

        public static async Task AddJobAssignmentAsync(McmaApiRequest request, McmaApiResponse response)
        {
            Logger.Debug(nameof(AddJobAssignmentAsync));
            Logger.Debug(request.ToMcmaJson().ToString());

            var jobAssignment = request.JsonBody?.ToMcmaObject<JobAssignment>();
            if (jobAssignment == null)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.StatusMessage = "Missing request body.";
                return;
            }

            var jobAssignmentId = request.StageVariables["PublicUrl"] + "/job-assignments/" + Guid.NewGuid();
            jobAssignment.Id = jobAssignmentId;
            jobAssignment.Status = "NEW";
            jobAssignment.DateCreated = DateTime.UtcNow;
            jobAssignment.DateModified = jobAssignment.DateCreated;

            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            await table.PutAsync<JobAssignment>(jobAssignmentId, jobAssignment);

            response.StatusCode = (int)HttpStatusCode.Created;
            response.Headers["Location"] = jobAssignment.Id;
            response.JsonBody = jobAssignment.ToMcmaJson();

            Logger.Debug(response.ToMcmaJson().ToString());

            // invoking worker lambda function that will create a jobAssignment assignment for this new jobAssignment
            var lambdaClient = new AmazonLambdaClient();
            var invokeRequest = new InvokeRequest
            {
                FunctionName = request.StageVariables["WorkerLambdaFunctionName"],
                InvocationType = "Event",
                LogType = "None",
                Payload = new { action = "ProcessJobAssignment", request = request, jobAssignmentId = jobAssignmentId }.ToMcmaJson().ToString()
            };

            await lambdaClient.InvokeAsync(invokeRequest);
        }

        public static async Task GetJobAssignmentAsync(McmaApiRequest request, McmaApiResponse response) 
        {
            Logger.Debug(nameof(GetJobAssignmentAsync));
            Logger.Debug(request.ToMcmaJson().ToString());

            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            var jobAssignmentId = request.StageVariables["PublicUrl"] + request.Path;

            var jobAssignment = await table.GetAsync<JobAssignment>(jobAssignmentId);
            response.JsonBody = jobAssignment != null ? jobAssignment.ToMcmaJson() : null;

            if (response.JsonBody == null)
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.StatusMessage = "No resource found on path '" + request.Path + "'.";
            }
        }
        
        public static async Task DeleteJobAssignmentAsync(McmaApiRequest request, McmaApiResponse response)
        {
            Logger.Debug(nameof(DeleteJobAssignmentAsync));
            Logger.Debug(request.ToMcmaJson().ToString());

            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            var jobAssignmentId = request.StageVariables["PublicUrl"] + request.Path;

            var jobAssignment = await table.GetAsync<JobAssignment>(jobAssignmentId);
            if (jobAssignment == null)
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.StatusMessage = "No resource found on path '" + request.Path + "'.";
                return;
            }

            await table.DeleteAsync<JobAssignment>(jobAssignmentId);
        }
    }
}
