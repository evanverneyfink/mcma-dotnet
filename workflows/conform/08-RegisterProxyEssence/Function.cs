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
using Mcma.Core.Serialization;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]

namespace Mcma.Aws.Workflows.Conform.RegisterProxyEssence
{
    public class Function
    {
        private static readonly string SERVICE_REGISTRY_URL = Environment.GetEnvironmentVariable(nameof(SERVICE_REGISTRY_URL));
        
        private McmaHttpClient McmaHttp { get; } = new McmaHttpClient();

        private string GetTransformJobId(JToken @event)
        {
            return @event["data"]["transformJobId"].FirstOrDefault()?.ToString();
        }

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

        private BMEssence CreateBmEssence(BMContent bmContent, S3Locator location)
        {
            return new BMEssence
            {
                BMContent = bmContent.Id,
                Locations = new Locator[] {location}
            };
        }

        public async Task<JToken> Handler(JToken @event, ILambdaContext context)
        {
            var resourceManager = new ResourceManager(SERVICE_REGISTRY_URL);

            try
            {
                var jobData = new JobBase
                {
                    Status = "RUNNING",
                    Progress = 63
                };
                await resourceManager.SendNotificationAsync(jobData, @event["notificationEndpoint"].ToMcmaObject<NotificationEndpoint>());
            }
            catch (Exception error)
            {
                Console.WriteLine("Failed to send notification: {0}", error);
            }
            
            var transformJobId = GetTransformJobId(@event);

            if (transformJobId == null)
                return @event["data"]["bmEssence"];

            var response = await McmaHttp.GetAsync(transformJobId);
            var transformJob = await response.EnsureSuccessStatusCode().Content.ReadAsObjectFromJsonAsync<TransformJob>();

            if (!transformJob.JobOutput.TryGet<S3Locator>("outputFile", out var outputFile))
                throw new Exception("Unable to get outputFile from AmeJob output.");

            var s3Bucket = outputFile.AwsS3Bucket;
            var s3Key = outputFile.AwsS3Key;

            var bmc = await GetBmContentAsync(@event["data"]["bmContent"]?.ToString());

            var locator = new S3Locator
            {
                AwsS3Bucket = s3Bucket,
                AwsS3Key = s3Key
            };

            var bme = CreateBmEssence(bmc, locator);

            bme = await resourceManager.CreateAsync(bme);
            if (bme?.Id == null)
                throw new Exception("Failed to register BMEssence.");

            bmc.BmEssences.Add(bme.Id);

            bmc = await resourceManager.UpdateAsync(bmc);

            return bme.Id;
        }
    }
}