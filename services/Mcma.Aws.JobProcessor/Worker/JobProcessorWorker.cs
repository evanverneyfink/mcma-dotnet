using System;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Amazon.Lambda.Core;
using Mcma.Core;
using Mcma.Core.Serialization;

namespace Mcma.Aws.JobProcessor.Worker
{
    internal static class JobProcessorWorker
    {
        internal static async Task CreateJobAssignmentAsync(JobProcessorWorkerRequest @event)
        {
            Console.WriteLine("Creating job assignment for job process " + @event.JobProcessId);

            var mcmaHttp = new McmaHttpClient();
            var resourceManager = new ResourceManager(@event.Request.StageVariables["ServicesUrl"]);

            var table = new DynamoDbTable(@event.Request.StageVariables["TableName"]);

            var jobProcessId = @event.JobProcessId;

            Console.WriteLine("Getting job process " + @event.JobProcessId);
            var jobProcess = await table.GetAsync<JobProcess>(jobProcessId);

            try
            {
                var jobId = jobProcess.Job;
                
                Console.WriteLine("Job ID for job process " + @event.JobProcessId + " is " + jobId);

                if (string.IsNullOrEmpty(jobId))
                    throw new Exception("JobProcess is missing a job definition.");

                Job job;
                try
                {
                    Console.WriteLine("Getting job " + jobId);
                    var jobResponse = await mcmaHttp.GetAsync(jobId);
                    Console.WriteLine("Parsing job " + jobId + " from response");
                    job = await jobResponse.EnsureSuccessStatusCode().Content.ReadAsObjectFromJsonAsync<Job>();
                }
                catch
                {
                    throw new Exception("Failed to retrieve job definition from url '" +  jobId + "'");
                }

                var jobProfileId = job.JobProfile;
                
                Console.WriteLine("Job profile ID for job " + jobId + " is " + jobProfileId);

                if (string.IsNullOrEmpty(jobProfileId))
                    throw new Exception("Job is missing jobProfile");

                JobProfile jobProfile;
                try
                {
                    Console.WriteLine("Getting job profile " + jobProfileId);
                    var jobProfileResponse = await mcmaHttp.GetAsync(jobProfileId);
                    Console.WriteLine("Parsing job profile " + jobProfileId + " from repsonse");
                    jobProfile = await jobProfileResponse.EnsureSuccessStatusCode().Content.ReadAsObjectFromJsonAsync<JobProfile>();
                }
                catch
                {
                    throw new Exception("Failed to retrieve job profile from url '" + jobProfileId + "'");
                }

                Console.WriteLine("Validating job input for job " + jobId);
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

                Console.WriteLine("Loading services for job assignment");
                var services = await resourceManager.GetAsync<Service>();

                Service selectedService = null;
                ServiceResource jobAssignmentResource = null;
                
                foreach (var service in services)
                {
                    jobAssignmentResource = null;

                    if (service.JobType == job.Type)
                    {
                        Console.WriteLine("Matched service " + service.Name + " on job type");
                        if (service.Resources != null)
                        {
                            foreach (var serviceResource in service.Resources)
                                if (serviceResource.ResourceType == nameof(JobAssignment))
                                    jobAssignmentResource = serviceResource;
                        }
                        
                        if (jobAssignmentResource == null)
                            continue;
                        
                        Console.WriteLine("Matched service resource " + jobAssignmentResource.HttpEndpoint + ". Checking for matching job profile");

                        if (service.JobProfiles != null)
                        {
                            foreach (var serviceJobProfile in service.JobProfiles)
                            {
                                if (serviceJobProfile == job.JobProfile)
                                {
                                    Console.WriteLine("Matched job profile");
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
                
                Console.WriteLine("Submitting job assignment to " + jobAssignmentResource.HttpEndpoint);
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

            Console.WriteLine("Updating job process on completion");
            await table.PutAsync<JobProcess>(jobProcessId, jobProcess);

            Console.WriteLine("Sending job process notification on completion");
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
                Console.WriteLine(error);
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
                Console.WriteLine("Ignoring update of job process that tried to change state from " + jobProcess.Status + " to " + notificationJobData.Status);
                return;
            }

            jobProcess.Status = notificationJobData.Status;
            jobProcess.StatusMessage = notificationJobData.StatusMessage;
            jobProcess.Progress = notificationJobData.Progress;
            jobProcess.JobOutput = notificationJobData.JobOutput;
            jobProcess.DateModified = DateTime.UtcNow;

            await table.PutAsync<JobProcess>(jobProcessId, jobProcess);

            var resourceManager = new ResourceManager(@event.Request.StageVariables["ServicesUrl"]);

            await resourceManager.SendNotificationAsync(jobProcess, jobProcess.NotificationEndpoint);
        }
    }
}
