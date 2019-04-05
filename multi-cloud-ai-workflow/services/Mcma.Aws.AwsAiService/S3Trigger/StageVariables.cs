using System;
using System.Collections.Generic;
using System.Linq;
using Amazon.Lambda.Core;
using Mcma.Aws;

namespace Mcma.Aws.AwsAiService.S3Trigger
{
    public class StageVariables
    {
        public string TableName => Environment.GetEnvironmentVariable(nameof(TableName));
        public string PublicUrl => Environment.GetEnvironmentVariable(nameof(PublicUrl));
        public string ServicesUrl => Environment.GetEnvironmentVariable(nameof(ServicesUrl));
        public string ServicesAuthType => Environment.GetEnvironmentVariable(nameof(ServicesAuthType));
        public string ServicesAuthContext => Environment.GetEnvironmentVariable(nameof(ServicesAuthContext));
        public string WorkerFunctionName => Environment.GetEnvironmentVariable(nameof(WorkerFunctionName));
        public string ServiceOutputBucket => Environment.GetEnvironmentVariable(nameof(ServiceOutputBucket));

        public IDictionary<string, string> ToDictionary() => typeof(StageVariables).GetProperties().ToDictionary(p => p.Name, p => p.GetValue(this)?.ToString());
    }
}