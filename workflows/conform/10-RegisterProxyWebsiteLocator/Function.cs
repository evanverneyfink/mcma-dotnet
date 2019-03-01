using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization;
using Amazon.S3;
using Amazon.S3.Model;
using Mcma.Aws;
using Mcma.Core;
using Mcma.Core.Logging;
using Mcma.Core.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]

namespace Mcma.Aws.Workflows.Conform.RegisterProxyWebsiteLocator
{
    public class Function
    {
        private static readonly string SERVICE_REGISTRY_URL = Environment.GetEnvironmentVariable(nameof(SERVICE_REGISTRY_URL));
        
        private McmaHttpClient McmaHttp { get; } = new McmaHttpClient();

        private async Task<BMContent> GetBmContentAsync(string url)
        {
            var response = await McmaHttp.GetAsync(url);
            return await response.EnsureSuccessStatusCode().Content.ReadAsObjectFromJsonAsync<BMContent>();
        }

        private async Task<BMEssence> GetBmEssenceAsync(string url)
        {
            var response = await McmaHttp.GetAsync(url);
            return await response.EnsureSuccessStatusCode().Content.ReadAsObjectFromJsonAsync<BMEssence>();
        }

        public async Task<JToken> Handler(JToken @event, ILambdaContext context)
        {
            var resourceManager = AwsEnvironment.GetAwsV4ResourceManager();

            try
            {
                var jobData = new JobBase
                {
                    Status = "RUNNING",
                    Progress = 81
                };
                await resourceManager.SendNotificationAsync(jobData, @event["notificationEndpoint"].ToMcmaObject<NotificationEndpoint>());
            }
            catch (Exception error)
            {
                Logger.Error("Failed to send notification: {0}", error);
            }

            var bme = await GetBmEssenceAsync(@event["data"]["bmEssence"]?.ToString());

            bme.Locations = new Locator[] { @event["data"]["websiteFile"]?.ToMcmaObject<S3Locator>() };

            bme = await resourceManager.UpdateAsync(bme);

            return bme.Id;
        }
    }
}