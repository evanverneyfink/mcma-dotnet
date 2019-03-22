using Newtonsoft.Json;
using Amazon.Lambda.Core;
using Mcma.Core;
using Mcma.Api;
using Mcma.Aws.Api;

namespace Mcma.Aws.AmeService.Worker
{
    public class AmeServiceWorkerRequest
    {
        public string Action { get; set; }

        public string JobAssignmentId { get; set; }

        public Notification Notification { get; set; }

        public ApiGatewayRequest Request { get; set; }
    }
}