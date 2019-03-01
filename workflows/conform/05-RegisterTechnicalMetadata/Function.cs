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

namespace Mcma.Aws.Workflows.Conform.RegisterTechnicalMetadata
{
    public class Function
    {
        private McmaHttpClient McmaHttp { get; } = new McmaHttpClient();

        private static readonly string SERVICE_REGISTRY_URL = Environment.GetEnvironmentVariable(nameof(SERVICE_REGISTRY_URL));
        
        private string GetAmeJobId(JToken @event)
        {
            return @event["data"]["ameJobId"].FirstOrDefault()?.ToString();
        }

        private async Task<BMContent> GetBmContentAsync(string url)
        {
            var response = await McmaHttp.GetAsync(url);
            return await response.EnsureSuccessStatusCode().Content.ReadAsObjectFromJsonAsync<BMContent>();
        }

        private BMEssence CreateBmEssence(BMContent bmContent, S3Locator location, JToken mediaInfo)
        {
            return new BMEssence
            {
                BMContent = bmContent.Id,
                Locations = new Locator[] {location},
                ["technicalMetadata"] = mediaInfo
            };
        }

        public async Task<JToken> Handler(JToken @event, ILambdaContext context)
        {
            var resourceManager = AwsEnvironment.GetAwsV4ResourceManager();

            try
            {
                var jobData = new JobBase
                {
                    Status = "RUNNING",
                    Progress = 36
                };
                await resourceManager.SendNotificationAsync(jobData, @event["notificationEndpoint"].ToMcmaObject<NotificationEndpoint>());
            }
            catch (Exception error)
            {
                Logger.Error("Failed to send notification: {0}", error);
            }

            var ameJobId = GetAmeJobId(@event);
            if (ameJobId == null)
                throw new Exception("Failed to obtain AmeJob ID");
            Logger.Debug("[AmeJobID]: " + ameJobId);

            var response = await McmaHttp.GetAsync(ameJobId);
            var ameJob = await response.EnsureSuccessStatusCode().Content.ReadAsObjectFromJsonAsync<AmeJob>();

            if (!ameJob.JobOutput.TryGet<S3Locator>("outputFile", out var outputFile))
                throw new Exception("Unable to get outputFile from AmeJob output.");

            var s3Bucket = outputFile.AwsS3Bucket;
            var s3Key = outputFile.AwsS3Key;
            GetObjectResponse s3Object;
            try
            {
                var s3 = await outputFile.GetClientAsync();
                s3Object = await s3.GetObjectAsync(new GetObjectRequest
                {
                    BucketName = s3Bucket,
                    Key = s3Key
                });
            }
            catch (Exception error)
            {
                throw new Exception("Unable to get media info file in bucket '" + s3Bucket + "' with key '" + s3Key + " due to error: " + error);
            }
            var mediaInfo = JToken.Parse(await new StreamReader(s3Object.ResponseStream).ReadToEndAsync());

            var bmc = await GetBmContentAsync(@event["data"]["bmContent"].ToString());

            Logger.Debug("[BMContent]: " + bmc.ToMcmaJson());

            var bme = CreateBmEssence(bmc, @event["data"]["repositoryFile"].ToMcmaObject<S3Locator>(), mediaInfo);

            bme = await resourceManager.CreateAsync(bme);
            if (bme.Id == null)
                throw new Exception("Failed to register BMEssence");
            Logger.Debug("[BMEssence ID]: " + bme.Id);

            bmc.BmEssences.Add(bme.Id);

            bmc = await resourceManager.UpdateAsync<BMContent>(bmc);

            return string.Empty;
        }
    }
}