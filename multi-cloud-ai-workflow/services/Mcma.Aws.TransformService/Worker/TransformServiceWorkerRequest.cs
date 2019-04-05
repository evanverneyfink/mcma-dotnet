using Newtonsoft.Json;
using Amazon.Lambda.Core;
using Mcma.Core;
using Mcma.Api;
using Mcma.Aws.Api;
using System.Collections.Generic;

namespace Mcma.Aws.TransformService.Worker
{
    public class TransformServiceWorkerRequest : IStageVariableProvider
    {
        public string Action { get; set; }

        public string JobAssignmentId { get; set; }

        public Notification Notification { get; set; }

        public IDictionary<string, string> StageVariables { get; set; }
    }
}
