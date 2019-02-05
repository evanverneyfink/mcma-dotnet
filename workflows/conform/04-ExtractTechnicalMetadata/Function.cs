using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization;
using Amazon.S3;
using Amazon.S3.Model;
using Amazon.StepFunctions;
using Amazon.StepFunctions.Model;
using Mcma.Aws;
using Mcma.Core;
using Mcma.Core.Serialization;
using Newtonsoft.Json.Linq;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]

namespace Mcma.Aws.Workflows.Conform.ExtractTechnicalMetadata
{
    public class Function
    {
        private static readonly string SERVICE_REGISTRY_URL = Environment.GetEnvironmentVariable(nameof(SERVICE_REGISTRY_URL));
        private static readonly string TEMP_BUCKET = Environment.GetEnvironmentVariable(nameof(TEMP_BUCKET));
        private static readonly string ACTIVITY_CALLBACK_URL = Environment.GetEnvironmentVariable(nameof(ACTIVITY_CALLBACK_URL));
        private static readonly string ACTIVITY_ARN = Environment.GetEnvironmentVariable(nameof(ACTIVITY_ARN));

        public async Task Handler(JToken @event, ILambdaContext context)
        {
            var resourceManager = new ResourceManager(SERVICE_REGISTRY_URL);

            try
            {
                var jobData = new JobBase
                {
                    Status = "RUNNING",
                    Progress = 27
                };
                await resourceManager.SendNotificationAsync(jobData, @event["notificationEndpoint"].ToMcmaObject<NotificationEndpoint>());
            }
            catch (Exception error)
            {
                Console.WriteLine("Failed to send notification: {0}", error);
            }

            var stepFunction = new AmazonStepFunctionsClient();
            var data = await stepFunction.GetActivityTaskAsync(new GetActivityTaskRequest
            {
                ActivityArn = ACTIVITY_ARN
            });

            var taskToken = data.TaskToken;
            if (taskToken == null)
                throw new Exception("Failed to obtain activity task");

            @event = JToken.Parse(data.Input);

            var jobProfiles = await resourceManager.GetAsync<JobProfile>(("name", "ExtractTechnicalMetadata"));

            var jobProfileId = jobProfiles?.FirstOrDefault()?.Id;

            if (jobProfileId == null)
                throw new Exception("JobProfile 'ExtractTechnicalMetadata' not found");

            var ameJob = new AmeJob
            {
                JobProfile = jobProfileId,
                JobInput = new JobParameterBag
                {
                    ["inputFile"] = @event["data"]["repositoryFile"],
                    ["outputLocation"] = new S3Locator
                    {
                        AwsS3Bucket = TEMP_BUCKET,
                        AwsS3KeyPrefix = "AmeJobResults/"
                    }
                },
                NotificationEndpoint = new NotificationEndpoint
                {
                    HttpEndpoint = ACTIVITY_CALLBACK_URL + "?taskToken=" + Uri.EscapeDataString(taskToken)
                }
            };

            ameJob = await resourceManager.CreateAsync(ameJob);
        }
    }
}