using System.Collections.Generic;
using Mcma.Core;

namespace Mcma.Aws.AmeService.Worker
{
    public class AmeServiceWorkerRequest : IStageVariableProvider
    {
        public string Action { get; set; }

        public string JobAssignmentId { get; set; }

        public Notification Notification { get; set; }

        public IDictionary<string, string> StageVariables { get; set; }
    }
}
