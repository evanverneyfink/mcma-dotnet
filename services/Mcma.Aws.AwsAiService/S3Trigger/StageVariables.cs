using Amazon.Lambda.Core;
using Mcma.Aws;

namespace Mcma.Aws.AwsAiService.S3Trigger
{
    public class StageVariables
    {
        public string TableName => System.Environment.GetEnvironmentVariable(nameof(TableName));
        public string PublicUrl => System.Environment.GetEnvironmentVariable(nameof(PublicUrl));
        public string ServicesUrl => System.Environment.GetEnvironmentVariable(nameof(ServicesUrl));
        public string WorkerLambdaFunctionName => System.Environment.GetEnvironmentVariable(nameof(WorkerLambdaFunctionName));
        public string ServiceOutputBucket => System.Environment.GetEnvironmentVariable(nameof(ServiceOutputBucket));
    }
}