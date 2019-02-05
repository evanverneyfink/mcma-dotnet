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

namespace Mcma.Aws.JobRepository.ApiHandler
{
    public static class JobRoutes
    {
        public static async Task GetJobsAsync(McmaApiRequest request, McmaApiResponse response)
        {
            Console.WriteLine(nameof(GetJobsAsync));
            Console.WriteLine(request.ToMcmaJson().ToString());
            
            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            response.JsonBody = (await table.GetAllAsync<Job>()).ToMcmaJson();

            Console.WriteLine(response.ToMcmaJson().ToString());
        }

        public static async Task AddJobAsync(McmaApiRequest request, McmaApiResponse response)
        {
            Console.WriteLine(nameof(AddJobAsync));
            Console.WriteLine(request.ToMcmaJson().ToString());

            var job = request.JsonBody?.ToMcmaObject<Job>();
            if (job == null)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.StatusMessage = "Missing request body.";
                return;
            }

            var jobId = request.StageVariables["PublicUrl"] + "/jobs/" + Guid.NewGuid();
            job.Id = jobId;
            job.Status = "NEW";
            job.DateCreated = DateTime.UtcNow;
            job.DateModified = job.DateCreated;

            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            await table.PutAsync<Job>(jobId, job);

            response.StatusCode = (int)HttpStatusCode.Created;
            response.Headers["Location"] = job.Id;
            response.JsonBody = job.ToMcmaJson();

            Console.WriteLine(response.ToMcmaJson().ToString());

            // invoking worker lambda function that will create a job process for this new job
            var lambdaClient = new AmazonLambdaClient();
            var invokeRequest = new InvokeRequest
            {
                FunctionName = request.StageVariables["WorkerLambdaFunctionName"],
                InvocationType = "Event",
                LogType = "None",
                Payload = new { action = "createJobProcess", request = request, jobId = jobId }.ToMcmaJson().ToString()
            };

            await lambdaClient.InvokeAsync(invokeRequest);
        }
        
        public static async Task GetJobAsync(McmaApiRequest request, McmaApiResponse response) 
        {
            Console.WriteLine(nameof(GetJobAsync));
            Console.WriteLine(request.ToMcmaJson().ToString());

            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            var jobId = request.StageVariables["PublicUrl"] + request.Path;

            var job = await table.GetAsync<Job>(jobId);
            response.JsonBody = job != null ? job.ToMcmaJson() : null;

            if (response.JsonBody == null)
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.StatusMessage = "No resource found on path '" + request.Path + "'.";
            }
        }
        
        public static async Task DeleteJobAsync(McmaApiRequest request, McmaApiResponse response)
        {
            Console.WriteLine(nameof(DeleteJobAsync));
            Console.WriteLine(request.ToMcmaJson().ToString());

            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            var jobId = request.StageVariables["PublicUrl"] + request.Path;

            var job = await table.GetAsync<Job>(jobId);
            if (job == null)
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.StatusMessage = "No resource found on path '" + request.Path + "'.";
                return;
            }

            await table.DeleteAsync<Job>(jobId);

            // invoking worker lambda function that will delete the JobProcess created for this Job
            if (!string.IsNullOrEmpty(job.JobProcess)) {
                var lambdaClient = new AmazonLambdaClient();
                var invokeRequest = new InvokeRequest
                {
                    FunctionName = request.StageVariables["WorkerLambdaFunctionName"],
                    InvocationType = "Event",
                    LogType = "None",
                    Payload = new { action = "deleteJobProcess", request = request, jobProcessId = job.JobProcess }.ToMcmaJson().ToString()
                };

                await lambdaClient.InvokeAsync(invokeRequest);
            }
        }
        
        public static async Task StopJobAsync(McmaApiRequest request, McmaApiResponse response)
        {
            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            var jobId = request.StageVariables["PublicUrl"] + "/jobs/" + request.PathVariables["id"];

            var job = await table.GetAsync<Job>(jobId);
            if (job == null)
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.StatusMessage = "No resource found on path '" + request.Path + "'";
                return;
            }

            response.StatusCode = (int)HttpStatusCode.NotImplemented;
            response.StatusMessage = "Stopping job is not implemented";
        }
        
        public static async Task CancelJobAsync(McmaApiRequest request, McmaApiResponse response)
        {
            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            var jobId = request.StageVariables["PublicUrl"] + "/jobs/" + request.PathVariables["id"];

            var job = await table.GetAsync<Job>(jobId);
            if (job == null)
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.StatusMessage = "No resource found on path '" + request.Path + "'";
                return;
            }

            response.StatusCode = (int)HttpStatusCode.NotImplemented;
            response.StatusMessage = "Canceling job is not implemented";
        }

        public static async Task ProcessNotificationAsync(McmaApiRequest request, McmaApiResponse response)
        {
            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            var jobId = request.StageVariables["PublicUrl"] + "/jobs/" + request.PathVariables["id"];

            var job = await table.GetAsync<Job>(jobId);
            if (job == null)
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.StatusMessage = "No resource found on path '" + request.Path + "'";
                return;
            }

            var notification = request.JsonBody?.ToMcmaObject<Notification>();

            if (notification == null)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.StatusMessage = "Missing notification in request body";
                return;
            }

            if (job.JobProcess != notification.Source)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.StatusMessage = "Unexpected notification from '" + notification.Source + "'.";
                return;
            }

            var lambdaClient = new AmazonLambdaClient();
            var invokeRequest = new InvokeRequest
            {
                FunctionName = request.StageVariables["WorkerLambdaFunctionName"],
                InvocationType = "Event",
                LogType = "None",
                Payload = new { action = "processNotification", request = request, jobId = jobId, notification = notification }.ToMcmaJson().ToString()
            };

            await lambdaClient.InvokeAsync(invokeRequest);
        }
    }
}
