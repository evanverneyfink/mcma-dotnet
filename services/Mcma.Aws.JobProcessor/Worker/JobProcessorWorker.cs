using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Amazon.Lambda.Core;
using Mcma.Core;
using Mcma.Core.Serialization;
using Mcma.Core.Logging;

namespace Mcma.Aws.JobProcessor.Worker
{
    internal static class JobProcessorWorker
    {
        internal static async Task CreateJobAssignmentAsync(JobProcessorWorkerRequest @event)
        {
            Logger.Debug("Creating job assignment for job process " + @event.JobProcessId);

            var mcmaHttp = new McmaHttpClient();
            var resourceManager = @event.Request.GetAwsV4ResourceManager();

            var table = new DynamoDbTable(@event.Request.StageVariables["TableName"]);

            var jobProcessId = @event.JobProcessId;

            Logger.Debug("Getting job process " + @event.JobProcessId);
            var jobProcess = await table.GetAsync<JobProcess>(jobProcessId);

            try
            {
                var jobId = jobProcess.Job;
                
                Logger.Debug("Job ID for job process " + @event.JobProcessId + " is " + jobId);

                if (string.IsNullOrEmpty(jobId))
                    throw new Exception("JobProcess is missing a job definition.");

                Job job;
                try
                {
                    Logger.Debug("Getting job " + jobId);
                    var jobResponse = await mcmaHttp.GetAsync(jobId);
                    Logger.Debug("Parsing job " + jobId + " from response");
                    job = await jobResponse.EnsureSuccessStatusCode().Content.ReadAsObjectFromJsonAsync<Job>();
                }
                catch
                {
                    throw new Exception("Failed to retrieve job definition from url '" +  jobId + "'");
                }

                var jobProfileId = job.JobProfile;
                
                Logger.Debug("Job profile ID for job " + jobId + " is " + jobProfileId);

                if (string.IsNullOrEmpty(jobProfileId))
                    throw new Exception("Job is missing jobProfile");

                JobProfile jobProfile;
                try
                {
                    Logger.Debug("Getting job profile " + jobProfileId);
                    var jobProfileResponse = await mcmaHttp.GetAsync(jobProfileId);
                    Logger.Debug("Parsing job profile " + jobProfileId + " from repsonse");
                    jobProfile = await jobProfileResponse.EnsureSuccessStatusCode().Content.ReadAsObjectFromJsonAsync<JobProfile>();
                }
                catch
                {
                    throw new Exception("Failed to retrieve job profile from url '" + jobProfileId + "'");
                }

                Logger.Debug("Validating job input for job " + jobId);
                var jobInput = job.JobInput;
                if (jobInput == null)
                    throw new Exception("Job is missing jobInput");

                if (jobProfile.InputParameters != null)
                {
                    foreach (var parameter in jobProfile.InputParameters)
                    {
                        if (!jobInput.ContainsKey(parameter.ParameterName))
                            throw new Exception("jobInput is missing required input parameter '" + parameter.ParameterName + "'");
                    }
                }

                Logger.Debug("Loading services for job assignment");
                var services = await resourceManager.GetAsync<Service>();

                Service selectedService = null;
                ResourceEndpoint jobAssignmentResource = null;
                
                foreach (var service in services)
                {
                    jobAssignmentResource = null;

                    if (service.JobType == job.Type)
                    {
                        Logger.Debug("Matched service " + service.Name + " on job type");
                        if (service.Resources != null)
                        {
                            foreach (var serviceResource in service.Resources)
                                if (serviceResource.ResourceType == nameof(JobAssignment))
                                    jobAssignmentResource = serviceResource;
                        }
                        
                        if (jobAssignmentResource == null)
                            continue;
                        
                        Logger.Debug("Matched service resource " + jobAssignmentResource.HttpEndpoint + ". Checking for matching job profile");

                        if (service.JobProfiles != null)
                        {
                            foreach (var serviceJobProfile in service.JobProfiles)
                            {
                                if (serviceJobProfile == job.JobProfile)
                                {
                                    Logger.Debug("Matched job profile");
                                    selectedService = service;
                                }
                            }
                        }
                    }

                    if (selectedService != null)
                        break;
                }

                if (jobAssignmentResource == null)
                    throw new Exception("Failed to find service that could execute the " + job.GetType().Name);

                var jobAssignment = new JobAssignment {Job = jobProcess.Job, NotificationEndpoint = new NotificationEndpoint{HttpEndpoint = jobProcessId + "/notifications"}};
                
                Logger.Debug("Submitting job assignment to " + jobAssignmentResource.HttpEndpoint);
                var response = await mcmaHttp.PostAsJsonAsync(jobAssignmentResource.HttpEndpoint, jobAssignment);
                jobAssignment = await response.EnsureSuccessStatusCode().Content.ReadAsObjectFromJsonAsync<JobAssignment>();

                jobProcess.Status = "SCHEDULED";
                jobProcess.JobAssignment = jobAssignment.Id;
            }
            catch (Exception error)
            {
                jobProcess.Status = "FAILED";
                jobProcess.StatusMessage = error.Message;
            }

            jobProcess.DateModified = DateTime.UtcNow;

            Logger.Debug("Updating job process on completion");
            await table.PutAsync<JobProcess>(jobProcessId, jobProcess);

            Logger.Debug("Sending job process notification on completion");
            await resourceManager.SendNotificationAsync(jobProcess, jobProcess.NotificationEndpoint);
        }

        internal static async Task DeleteJobAssignmentAsync(JobProcessorWorkerRequest @event)
        {
            var jobAssignmentId = @event.JobAssignmentId;

            try
            {
                var mcmaHttp = new McmaHttpClient();

                await mcmaHttp.DeleteAsync(jobAssignmentId);
            }
            catch (Exception error)
            {
                Logger.Exception(error);
            }
        }

        internal static async Task ProcessNotificationAsync(JobProcessorWorkerRequest @event)
        {
            var jobProcessId = @event.JobProcessId;
            var notification = @event.Notification;
            var notificationJobData = notification.Content.ToMcmaObject<JobBase>();

            var table = new DynamoDbTable(@event.Request.StageVariables["TableName"]);

            var jobProcess = await table.GetAsync<JobProcess>(jobProcessId);

            // not updating job if it already was marked as completed or failed.
            if (jobProcess.Status == "COMPLETED" || jobProcess.Status == "FAILED")
            {
                Logger.Warn("Ignoring update of job process that tried to change state from " + jobProcess.Status + " to " + notificationJobData.Status);
                return;
            }

            jobProcess.Status = notificationJobData.Status;
            jobProcess.StatusMessage = notificationJobData.StatusMessage;
            jobProcess.Progress = notificationJobData.Progress;
            jobProcess.JobOutput = notificationJobData.JobOutput;
            jobProcess.DateModified = DateTime.UtcNow;

            await table.PutAsync<JobProcess>(jobProcessId, jobProcess);

            var resourceManager = @event.Request.GetAwsV4ResourceManager();

            await resourceManager.SendNotificationAsync(jobProcess, jobProcess.NotificationEndpoint);
        }
    }
}
