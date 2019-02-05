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

namespace Mcma.Aws.Workflows.Conform.ValidateWorkflowInput
{
    public class Function
    {
        private static readonly string SERVICE_REGISTRY_URL = Environment.GetEnvironmentVariable(nameof(SERVICE_REGISTRY_URL));

        public async Task<JToken> Handler(JToken @event, ILambdaContext context)
        {
            if (@event == null)
                throw new Exception("Missing workflow input");

            var resourceManager = new ResourceManager(SERVICE_REGISTRY_URL);

            try
            {
                var jobData = new JobBase
                {
                    Status = "RUNNING",
                    Progress = 0
                };
                await resourceManager.SendNotificationAsync(jobData, @event["notificationEndpoint"].ToMcmaObject<NotificationEndpoint>());
            }
            catch (Exception error)
            {
                Console.WriteLine("Failed to send notification: {0}", error);
            }

            var input = @event["input"];
            if (input == null)
                throw new Exception("Missing workflow input");

            var metadata = input["metadata"]?.ToMcmaObject<DescriptiveMetadata>();

            if (metadata == null)
                throw new Exception("Missing input.metadata");

            if (metadata.Name == null)
                throw new Exception("Missing input.metadata.name");

            if (metadata.Description == null)
                throw new Exception("Missing input.metadata.description");

            var inputFile = input["inputFile"]?.ToMcmaObject<S3Locator>();
            
            if (inputFile == null)
                throw new Exception("Missing input.inputFile");

            var s3Bucket = inputFile.AwsS3Bucket;
            var s3Key = inputFile.AwsS3Key;

            var client = new AmazonS3Client();

            GetObjectMetadataResponse data;

            try
            {
                data = await client.GetObjectMetadataAsync(new GetObjectMetadataRequest
                {
                    BucketName = s3Bucket,
                    Key = s3Key
                });
            }
            catch (Exception error)
            {
                throw new Exception("Unable to read input file in bucket '" + s3Bucket + "' with key '" + s3Key + "' due to error: " + error.Message);
            }

            return JObject.FromObject(data.Metadata.Keys.ToDictionary(k => k, k => data.Metadata[k]));
        }
    }
}