using Newtonsoft.Json;
using Amazon.Lambda.Core;
using Mcma.Core;
using Mcma.Api;

namespace Mcma.Aws.TransformService.Worker
{
    public class TransformServiceWorkerRequest
    {
        public string Action { get; set; }

        public string JobAssignmentId { get; set; }

        public Notification Notification { get; set; }

        public McmaApiRequest Request { get; set; }
    }
}
