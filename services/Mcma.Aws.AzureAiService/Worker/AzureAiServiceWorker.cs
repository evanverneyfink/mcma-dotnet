using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Amazon;
using Amazon.Lambda.Core;
using Amazon.S3;
using Amazon.S3.Model;
using Mcma.Core;
using Mcma.Core.Serialization;

namespace Mcma.Aws.AzureAiService.Worker
{
    internal class AzureConfig
    {
        public AzureConfig(AzureAiServiceWorkerRequest @event)
        {
            ApiUrl = @event.Request.StageVariables["AzureApiUrl"];
            Location = @event.Request.StageVariables["AzureLocation"];
            AccountID = @event.Request.StageVariables["AzureAccountID"];
            SubscriptionKey = @event.Request.StageVariables["AzureSubscriptionKey"];
        }

        public string ApiUrl { get; }

        public string Location { get; }

        public string AccountID { get; }

        public string SubscriptionKey { get; }
    }

    internal static class AzureAiServiceWorker
    {
        public const string JOB_PROFILE_TRANSCRIBE_AUDIO = "AzureTranscribeAudio";
        public const string JOB_PROFILE_TRANSLATE_TEXT = "AzureTranslateText";
        public const string JOB_PROFILE_EXTRACT_ALL_AI_METADATA = "AzureExtractAllAIMetadata";

        private static string REKO_SNS_ROLE_ARN = Environment.GetEnvironmentVariable("REKO_SNS_ROLE_ARN");
        private static string SNS_TOPIC_ARN = Environment.GetEnvironmentVariable("SNS_TOPIC_ARN");

        internal static async Task ProcessJobAssignmentAsync(AzureAiServiceWorkerRequest @event)
        {
            var resourceManager = new ResourceManager(@event.Request.StageVariables["ServicesUrl"]);
            var table = new DynamoDbTable(@event.Request.StageVariables["TableName"]);
            var jobAssignmentId = @event.JobAssignmentId;
            var azure = new AzureConfig(@event);

            try
            {
                // 1. Setting job assignment status to RUNNING
                await UpdateJobAssignmentStatusAsync(resourceManager, table, jobAssignmentId, "RUNNING", null);

                // 2. Retrieving WorkflowJob
                var job = await RetrieveJobAsync(table, jobAssignmentId);

                // 3. Retrieve JobProfile
                var jobProfile = await RetrieveJobProfileAsync(job);

                // 4. Retrieve job inputParameters
                var jobInput = job.JobInput;

                // 5. Check if we support jobProfile and if we have required parameters in jobInput
                ValidateJobProfile(jobProfile, jobInput);

                S3Locator inputFile;
                if (!jobInput.TryGet<S3Locator>(nameof(inputFile), out inputFile))
                    throw new Exception("Invalid or missing input file.");

                string mediaFileUrl;
                if (!string.IsNullOrWhiteSpace(inputFile.HttpEndpoint))
                {
                    mediaFileUrl = inputFile.HttpEndpoint;
                }
                else
                {
                    var bucketLocation = await inputFile.GetBucketLocationAsync();
                    var s3SubDomain = !string.IsNullOrWhiteSpace(bucketLocation) ? $"s3-{bucketLocation}" : "s3";
                    mediaFileUrl = $"https://{s3SubDomain}.amazonaws.com/{inputFile.AwsS3Bucket}/{inputFile.AwsS3Key}";
                }

                switch (jobProfile.Name)
                {
                    case JOB_PROFILE_TRANSCRIBE_AUDIO:
                    case JOB_PROFILE_TRANSLATE_TEXT:
                        throw new NotImplementedException($"{jobProfile.Name} profile has not yet been implemented for Azure.");
                    case JOB_PROFILE_EXTRACT_ALL_AI_METADATA:
                        var authTokenUrl = azure.ApiUrl + "/auth/" + azure.Location + "/Accounts/" + azure.AccountID + "/AccessToken?allowEdit=true";
                        var customHeaders = new Dictionary<string, string> { ["Ocp-Apim-Subscription-Key"] = azure.SubscriptionKey };

                        Console.WriteLine($"Generate Azure Video Indexer Token: Doing a GET on {authTokenUrl}");
                        var mcmaHttp = new McmaHttpClient();
                        var response = await mcmaHttp.GetAsync(authTokenUrl, customHeaders);

                        var apiToken = response.Content.ReadAsJsonAsync();
                        Console.WriteLine($"Azure API Token: {apiToken}");

                        var postVideoUrl = azure.ApiUrl + "/" + azure.Location + "/Accounts/" + azure.AccountID + "/Videos?accessToken=" + apiToken + "&name=" + inputFile.AwsS3Key + "&callbackUrl=" + jobAssignmentId + "/notifications&videoUrl=" + mediaFileUrl + "&fileName=" + inputFile.AwsS3Key;
                        
                        Console.WriteLine($"Call Azure Video Indexer API: Doing a POST on {postVideoUrl}");
                        var postVideoResponse = await mcmaHttp.PostAsync(postVideoUrl, null);

                        if ((int)postVideoResponse.StatusCode != 200)
                            Console.WriteLine($"Azure Video Indexer - Error processing the video: ${response.StatusCode} ${(response.Content != null ? await response.Content.ReadAsStringAsync() : "[no body]")}");
                        else
                        {
                            var azureAssetInfo = await postVideoResponse.Content.ReadAsJsonAsync();
                            Console.WriteLine("azureAssetInfo: ", azureAssetInfo);

                            try
                            {
                                var jobOutput = new JobParameterBag();
                                jobOutput["jobInfo"] = azureAssetInfo;

                                await UpdateJobAssignmentWithOutputAsync(table, jobAssignmentId, jobOutput);
                            }
                            catch (Exception error)
                            {
                                Console.WriteLine("Error updating the job", error);
                            }
                        }
                        break;
                }

            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);

                try
                {
                    await UpdateJobAssignmentStatusAsync(resourceManager, table, jobAssignmentId, "FAILED", ex.Message);
                }
                catch (Exception innerEx)
                {
                    Console.WriteLine(innerEx);
                }
            }
        }

        public static async Task ProcessNotificationAsync(AzureAiServiceWorkerRequest @event)
        {
            var jobAssignmentId = @event.JobAssignmentId;

            var resourceManager = new ResourceManager(@event.Request.StageVariables["ServicesUrl"]);
            var table = new DynamoDbTable(@event.Request.StageVariables["TableName"]);
            var azure = new AzureConfig(@event);

            var azureVideoId = @event.Notification?.Id;
            var azureState = @event.Notification?.State;
            if (azureVideoId == null || azureState == null)
            {
                Console.WriteLine("POST is not coming from Azure Video Indexer. Expected notification to have id and state properties.");
                return;
            }

            try
            {
                var authTokenUrl = azure.ApiUrl + "/auth/" + azure.Location + "/Accounts/" + azure.AccountID + "/AccessToken?allowEdit=true";
                var customHeaders = new Dictionary<string, string> { ["Ocp-Apim-Subscription-Key"] = azure.SubscriptionKey };

                Console.WriteLine($"Generate Azure Video Indexer Token: Doing a GET on {authTokenUrl}");
                var mcmaHttp = new McmaHttpClient();
                var response = await mcmaHttp.GetAsync(authTokenUrl, customHeaders);

                var apiToken = response.Content.ReadAsJsonAsync();
                Console.WriteLine($"Azure API Token: {apiToken}");
                
                var metadataFromAzureVideoIndexer = azure.ApiUrl + "/" + azure.Location + "/Accounts/" + azure.AccountID + "/Videos/" + azureVideoId + "/Index?accessToken=" + apiToken + "&language=English";

                Console.WriteLine("Get the azure video metadata : Doing a GET on  : ", metadataFromAzureVideoIndexer);
                var indexedVideoMetadataResponse = await mcmaHttp.GetAsync(metadataFromAzureVideoIndexer);

                var videoMetadata = await indexedVideoMetadataResponse.Content.ReadAsJsonAsync();
                Console.WriteLine("Azure AI video metadata : ", videoMetadata);

                //Need to hydrate the destination bucket from the job input
                var workflowJob = await RetrieveJobAsync(table, jobAssignmentId);

                //Retrieve job inputParameters
                var jobInput = workflowJob.JobInput;
                
                S3Locator outputLocation;
                if (!jobInput.TryGet<S3Locator>(nameof(outputLocation), out outputLocation))
                    throw new Exception("Invalid or missing output location.");

                var jobOutputBucket = outputLocation.AwsS3Bucket;
                var jobOutputKeyPrefix = outputLocation.AwsS3KeyPrefix != null ? outputLocation.AwsS3KeyPrefix : "";

                // get the info about the destination bucket to store the result of the job
                var s3Params = new PutObjectRequest
                {
                    BucketName = jobOutputBucket,
                    Key = jobOutputKeyPrefix + azureVideoId + "-" + Guid.NewGuid() + ".json",
                    ContentBody = videoMetadata.ToString(),
                    ContentType = "application/json"
                };

                var destS3 = await outputLocation.GetClientAsync();
                await destS3.PutObjectAsync(s3Params);

                //updating JobAssignment with jobOutput
                var jobOutput = workflowJob.JobOutput ??  new JobParameterBag();
                jobOutput["outputFile"] = new S3Locator
                {
                    AwsS3Bucket = s3Params.BucketName,
                    AwsS3Key = s3Params.Key
                };

                await UpdateJobAssignmentWithOutputAsync(table, jobAssignmentId, jobOutput);

                // Setting job assignment status to COMPLETED
                await UpdateJobAssignmentStatusAsync(resourceManager, table, jobAssignmentId, "COMPLETED");
            }
            catch (Exception error)
            {
                Console.WriteLine(error);

                try
                {
                    await UpdateJobAssignmentStatusAsync(resourceManager, table, jobAssignmentId, "FAILED", error.Message);
                }
                catch (Exception innerEx)
                {
                    Console.WriteLine(innerEx);
                }
            }
        }

        private static void ValidateJobProfile(JobProfile jobProfile, IDictionary<string, object> jobInput)
        {
            if (jobProfile.Name != JOB_PROFILE_TRANSCRIBE_AUDIO &&
                jobProfile.Name != JOB_PROFILE_TRANSLATE_TEXT &&
                jobProfile.Name != JOB_PROFILE_EXTRACT_ALL_AI_METADATA)
                throw new Exception("JobProfile '" + jobProfile.Name + "' is not supported");

            if (jobProfile.InputParameters != null)
                foreach (var parameter in jobProfile.InputParameters)
                    if (!jobInput.ContainsKey(parameter.ParameterName))
                        throw new Exception("jobInput misses required input parameter '" + parameter.ParameterName + "'");
        }

        private static async Task<JobProfile> RetrieveJobProfileAsync(Job job)
        {
            return await RetrieveResourceAsync<JobProfile>(job.JobProfile, "job.jobProfile");
        }

        private static async Task<Job> RetrieveJobAsync(DynamoDbTable table, string jobAssignmentId)
        {
            var jobAssignment = await GetJobAssignmentAsync(table, jobAssignmentId);

            return await RetrieveResourceAsync<Job>(jobAssignment.Job, "jobAssignment.job");
        }

        private static async Task<T> RetrieveResourceAsync<T>(string resource, string resourceName)
        {
            var mcmaHttp = new McmaHttpClient();
            try
            {
                var response = await mcmaHttp.GetAsync(resource);
                
                return await response.EnsureSuccessStatusCode().Content.ReadAsObjectFromJsonAsync<T>();
            }
            catch
            {
                throw new Exception("Failed to retrieve '" + resourceName + "' from url '" + resource + "'");
            }
        }

        private static async Task UpdateJobAssignmentWithOutputAsync(DynamoDbTable table, string jobAssignmentId, JobParameterBag jobOutput)
        {
            var jobAssignment = await GetJobAssignmentAsync(table, jobAssignmentId);
            jobAssignment.JobOutput = jobOutput;
            await PutJobAssignmentAsync(null, table, jobAssignmentId, jobAssignment);
        }

        private static async Task UpdateJobAssignmentStatusAsync(ResourceManager resourceManager, DynamoDbTable table, string jobAssignmentId, string status, string statusMessage = null)
        {
            var jobAssignment = await GetJobAssignmentAsync(table, jobAssignmentId);
            jobAssignment.Status = status;
            jobAssignment.StatusMessage = statusMessage;
            await PutJobAssignmentAsync(resourceManager, table, jobAssignmentId, jobAssignment);
        }

        private static async Task<JobAssignment> GetJobAssignmentAsync(DynamoDbTable table, string jobAssignmentId)
        {
            var jobAssignment = await table.GetAsync<JobAssignment>(jobAssignmentId);
            if (jobAssignment == null)
                throw new Exception("JobAssignment with id '" + jobAssignmentId + "' not found");
            return jobAssignment;
        }

        private static async Task PutJobAssignmentAsync(ResourceManager resourceManager, DynamoDbTable table, string jobAssignmentId, JobAssignment jobAssignment)
        {
            jobAssignment.DateModified = DateTime.UtcNow;
            await table.PutAsync<JobAssignment>(jobAssignmentId, jobAssignment);

            if (resourceManager != null)
                await resourceManager.SendNotificationAsync(jobAssignment, jobAssignment.NotificationEndpoint);
        }
    }
}
