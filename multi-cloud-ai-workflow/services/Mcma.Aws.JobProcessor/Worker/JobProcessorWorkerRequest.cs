using Newtonsoft.Json;
using Amazon.Lambda.Core;
using Mcma.Core;
using System.Collections.Generic;

namespace Mcma.Aws.JobProcessor.Worker
{
    public class JobProcessorWorkerRequest : IStageVariableProvider
    {
        public string Action { get; set; }

        public string JobProcessId { get; set; }

        public string JobAssignmentId { get; set; }

        public Notification Notification { get; set; }

        public IDictionary<string, string> StageVariables { get; set; }
    }
}
