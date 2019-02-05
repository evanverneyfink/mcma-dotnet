using System;
using System.Linq;
using System.Net;
using System.Reflection;
using System.Threading.Tasks;
using Amazon.Runtime;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Mcma.Aws;
using Mcma.Core;
using Mcma.Core.Serialization;
using Mcma.Api;
using Amazon.Lambda.Core;

namespace Mcma.Aws.ServiceRegistry.ApiHandler
{
    public static class ServiceRoutes
    {
        public static async Task GetServicesAsync(McmaApiRequest request, McmaApiResponse response)
        {
            Console.WriteLine(nameof(GetServicesAsync));
            Console.WriteLine(request.ToMcmaJson().ToString());

            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            var services = await table.GetAllAsync<Service>();

            if (request.QueryStringParameters.Any())
                services.Filter(request.QueryStringParameters);

            response.JsonBody = services.ToMcmaJson();
            
            Console.WriteLine(response.ToMcmaJson().ToString());
        }

        public static async Task AddServiceAsync(McmaApiRequest request, McmaApiResponse response)
        {
            Console.WriteLine(nameof(AddServiceAsync));
            Console.WriteLine(request.ToMcmaJson().ToString());

            var service = request.JsonBody?.ToMcmaObject<Service>();
            if (service == null)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.StatusMessage = "Missing request body.";
                return;
            }

            var serviceId = request.StageVariables["PublicUrl"] + "/services/" + Guid.NewGuid();
            
            service.Id = serviceId;
            service.DateCreated = DateTime.UtcNow;
            service.DateModified = service.DateCreated;

            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            await table.PutAsync<Service>(serviceId, service);

            response.StatusCode = (int)HttpStatusCode.Created;
            response.Headers["Location"] = service.Id;
            response.JsonBody = service.ToMcmaJson();
            
            Console.WriteLine(response.ToMcmaJson().ToString());
        }

        public static async Task GetServiceAsync(McmaApiRequest request, McmaApiResponse response)
        {
            Console.WriteLine(nameof(GetServiceAsync));
            Console.WriteLine(request.ToMcmaJson().ToString());

            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            var serviceId = request.StageVariables["PublicUrl"] + request.Path;

            response.JsonBody = (await table.GetAsync<Service>(serviceId)).ToMcmaJson();

            if (response.JsonBody == null)
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.StatusMessage = "No resource found on path '" + request.Path + "'.";
            }
        }

        public static async Task PutServiceAsync(McmaApiRequest request, McmaApiResponse response)
        {
            Console.WriteLine(nameof(PutServiceAsync));
            Console.WriteLine(request.ToMcmaJson().ToString());

            var service = request.JsonBody?.ToMcmaObject<Service>();
            if (service == null)
            {
                response.StatusCode = (int)HttpStatusCode.BadRequest;
                response.StatusMessage = "Missing request body.";
                return;
            }

            var table = new DynamoDbTable(request.StageVariables["TableName"]);
            
            var serviceId = request.StageVariables["PublicUrl"] + request.Path;
            service.Id = serviceId;
            service.DateModified = DateTime.UtcNow;
            if (!service.DateCreated.HasValue)
                service.DateCreated = service.DateModified;

            await table.PutAsync<Service>(serviceId, service);

            response.JsonBody = service.ToMcmaJson();
        }

        public static async Task DeleteServiceAsync(McmaApiRequest request, McmaApiResponse response)
        {
            Console.WriteLine(nameof(DeleteServiceAsync));
            Console.WriteLine(request.ToMcmaJson().ToString());

            var table = new DynamoDbTable(request.StageVariables["TableName"]);

            var serviceId = request.StageVariables["PublicUrl"] + request.Path;

            var service = await table.GetAsync<Service>(serviceId);
            if (service == null)
            {
                response.StatusCode = (int)HttpStatusCode.NotFound;
                response.StatusMessage = "No resource found on path '" + request.Path + "'.";
                return;
            }

            await table.DeleteAsync<Service>(serviceId);
        }
    }
}
