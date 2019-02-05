using System;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization;
using Amazon.S3;
using Amazon.S3.Model;
using Mcma.Aws;
using Mcma.Core;
using Mcma.Core.Serialization;
using Newtonsoft.Json.Linq;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]

namespace Mcma.Aws.Workflows.ProcessWorkflowCompletion
{
    public class Function
    {
        private static readonly string SERVICE_REGISTRY_URL = Environment.GetEnvironmentVariable(nameof(SERVICE_REGISTRY_URL));

        public async Task Handler(JToken @event, ILambdaContext context)
        {
            var resourceManager = new ResourceManager(SERVICE_REGISTRY_URL);

            try
            {
                var jobData = new JobBase
                {
                    Status = "COMPLETED",
                    Progress = 100
                };
                await resourceManager.SendNotificationAsync(jobData, @event["notificationEndpoint"].ToMcmaObject<NotificationEndpoint>());
            }
            catch (Exception error)
            {
                Console.WriteLine("Failed to send notification: {0}", error);
            }
        }
    }
}