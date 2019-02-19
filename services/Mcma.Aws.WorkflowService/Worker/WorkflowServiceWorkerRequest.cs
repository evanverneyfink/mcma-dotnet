using Newtonsoft.Json;
using Amazon.Lambda.Core;
using Mcma.Core;
using Mcma.Api;

namespace Mcma.Aws.WorkflowService.Worker
{
    public class WorkflowServiceWorkerRequest
    {
        public string Action { get; set; }

        public string JobAssignmentId { get; set; }

        public Notification Notification { get; set; }

        public McmaApiRequest Request { get; set; }
    }
}
