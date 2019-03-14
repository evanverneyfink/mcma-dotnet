using System;
using System.Collections.Generic;
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
using Newtonsoft.Json.Linq;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.Workflows.Ai.RegisterCelebritiesInfoAws
{
    public class Function
    {
        public async Task Handler(JToken @event, ILambdaContext context)
        {
            if (@event == null)
                throw new Exception("Missing workflow input");

            var resourceManager = AwsEnvironment.GetAwsV4ResourceManager();

            try
            {
                var jobData = new JobBase
                {
                    Status = "RUNNING",
                    ParallelProgress =  { ["detect-celebrities-aws"] = 80 }
                };
                await resourceManager.SendNotificationAsync(jobData, @event["notificationEndpoint"].ToMcmaObject<NotificationEndpoint>());
            }
            catch (Exception error)
            {
                Logger.Error("Failed to send notification: {0}", error);
            }

            // get ai job id (first non null entry in array)
            var jobId = @event["data"]["awsCelebritiesJobId"]?.FirstOrDefault(id => id != null)?.Value<string>();
            if (jobId == null)
                throw new Exception("Failed to obtain awsCelebritiesJobId");
            
            Logger.Debug("[awsCelebritiesJobId]:", jobId);

            // get result of ai job
            var job = await resourceManager.ResolveAsync<AIJob>(jobId);

            S3Locator outputFile;
            if (!job.JobOutput.TryGet<S3Locator>(nameof(outputFile), false, out outputFile))
                throw new Exception($"AI job '{jobId}' does not specify an output file.");

            // get media info
            var s3Bucket = outputFile.AwsS3Bucket;
            var s3Key = outputFile.AwsS3Key;
            GetObjectResponse s3Object;
            try
            {
                var s3Client = new AmazonS3Client();
                s3Object = await s3Client.GetObjectAsync(new GetObjectRequest
                {
                    BucketName = s3Bucket,
                    Key = s3Key,
                });
            }
            catch (Exception error)
            {
                throw new Exception("Unable to celebrities info file in bucket '" + s3Bucket + "' with key '" + s3Key + "'", error);
            }

            dynamic celebritiesResult = (await s3Object.ResponseStream.ReadJsonFromStreamAsync()).ToMcmaObject<McmaDynamicObject>();

            dynamic celebritiesMap = new McmaExpandoObject();

            List<McmaExpandoObject> celebritiesResultList = celebritiesResult.Celebrities.ToList();

            for (var i = 0; i < celebritiesResultList.Count;)
            {
                dynamic celebrity = celebritiesResultList[i];

                var prevCelebrity = celebritiesMap.HasProperty(celebrity.Celebrity.Name) ? celebritiesMap[celebrity.Celebrity.Name] : null;
                if ((prevCelebrity == null || celebrity.Timestamp - prevCelebrity.Timestamp > 3000) && celebrity.Celebrity.Confidence > 50)
                {
                    celebritiesMap[celebrity.Celebrity.Name] = celebrity;
                    i++;
                }
                else
                {
                    celebritiesResultList.RemoveAt(i);
                }
            }

            celebritiesResult.Celebrities = celebritiesResultList.ToArray();

            Logger.Debug("AWS Celebrities result", celebritiesResult.ToMcmaJson().ToString());

            dynamic bmContent = await resourceManager.ResolveAsync<BMContent>(@event["input"]["bmContent"].Value<string>());

            if (!bmContent.HasProperty("AwsAiMetadata", false))
                bmContent.AwsAiMetadata = new McmaExpandoObject();

            bmContent.AwsAiMetadata.Celebrities = celebritiesResult;

            await resourceManager.UpdateAsync(bmContent);

            try
            {
                var jobData = new JobBase
                {
                    Status = "RUNNING",
                    ParallelProgress =  { ["detect-celebrities-aws"] = 100 }
                };
                await resourceManager.SendNotificationAsync(jobData, @event["notificationEndpoint"].ToMcmaObject<NotificationEndpoint>());
            }
            catch (Exception error)
            {
                Logger.Error("Failed to send notification: {0}", error);
            }
        }
    }
}