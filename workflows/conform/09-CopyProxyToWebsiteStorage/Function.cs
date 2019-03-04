using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Amazon;
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
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.Workflows.Conform.CopyProxyToWebsiteStorage
{
    public class Function
    {
        private static readonly string SERVICE_REGISTRY_URL = Environment.GetEnvironmentVariable(nameof(SERVICE_REGISTRY_URL));
        private static readonly string WEBSITE_BUCKET = Environment.GetEnvironmentVariable(nameof(WEBSITE_BUCKET));
        
        private McmaHttpClient McmaHttp { get; } = new McmaHttpClient();

        private string GetTransformJobId(JToken @event)
        {
            return @event["data"]["transformJobId"].FirstOrDefault()?.ToString();
        }

        private async Task<BMEssence> GetBmEssenceAsync(string url)
        {
            var response = await McmaHttp.GetAsync(url);
            return await response.EnsureSuccessStatusCode().Content.ReadAsObjectFromJsonAsync<BMEssence>();
        }

        public async Task<S3Locator> Handler(JToken @event, ILambdaContext context)
        {
            var resourceManager = AwsEnvironment.GetAwsV4ResourceManager();

            try
            {
                var jobData = new JobBase
                {
                    Status = "RUNNING",
                    Progress = 72
                };
                await resourceManager.SendNotificationAsync(jobData, @event["notificationEndpoint"].ToMcmaObject<NotificationEndpoint>());
            }
            catch (Exception error)
            {
                Logger.Error("Failed to send notification: {0}", error);
            }

            var transformJobId = GetTransformJobId(@event);

            S3Locator outputFile;
            if (transformJobId == null)
            {
                var bme = await GetBmEssenceAsync(@event["data"]["bmEssence"]?.ToString());

                outputFile = (S3Locator)bme.Locations[0];
            }
            else
            {
                var response = await McmaHttp.GetAsync(transformJobId);
                var transformJob = await response.EnsureSuccessStatusCode().Content.ReadAsObjectFromJsonAsync<TransformJob>();

                outputFile = transformJob.JobOutput.Get<S3Locator>(nameof(outputFile));
            }

            var s3Bucket = WEBSITE_BUCKET;
            var s3Key = "media/" + Guid.NewGuid();

            var idxLastDot = outputFile.AwsS3Key.LastIndexOf(".");
            if (idxLastDot > 0)
                s3Key += outputFile.AwsS3Key.Substring(idxLastDot);

            var s3 = new AmazonS3Client();
            var data = await s3.GetBucketLocationAsync(s3Bucket);
            try
            {
                var copyParams = new CopyObjectRequest
                {
                    SourceBucket = outputFile.AwsS3Bucket,
                    SourceKey = outputFile.AwsS3Key,
                    DestinationBucket = s3Bucket,
                    DestinationKey = s3Key
                };
                var destS3 = new AmazonS3Client(RegionEndpoint.GetBySystemName(data.Location));
                await destS3.CopyObjectAsync(copyParams);
            }
            catch (Exception error)
            {
                throw new Exception("Unable to read input file in bucket '" + s3Bucket + "' with key '" + s3Key + "' due to error: " + error);
            }

            var s3SubDomain = !string.IsNullOrWhiteSpace(data.Location) ? $"s3-{data.Location}" : "s3";
            var httpEndpoint = "https://" + s3SubDomain + ".amazonaws.com/" + s3Bucket + "/" + s3Key;

            return new S3Locator
            {
                AwsS3Bucket = s3Bucket,
                AwsS3Key = s3Key,
                HttpEndpoint = httpEndpoint
            };
        }
    }
}