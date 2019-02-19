using System;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Core;
using Amazon.Lambda.Model;
using Amazon.Lambda.Serialization;
using Amazon.Lambda.S3Events;
using Newtonsoft.Json.Linq;
using Mcma.Aws;
using Mcma.Core.Serialization;
using System.Text;
using System.Text.RegularExpressions;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]

namespace Mcma.Aws.AwsAiService.S3Trigger
{

    public class Function
    {
        private StageVariables StageVariables { get; } = new StageVariables();

        public async Task Handler(S3Event @event, ILambdaContext context)
        {
            if (@event == null || @event.Records == null)
                return;

            foreach (var record in @event.Records)
            {
                try
                {
                    var awsS3Bucket = record.S3.Bucket.Name;
                    var awsS3Key = record.S3.Object.Key;

                    if (!Regex.IsMatch(awsS3Key, "^TranscriptionJob-[0-9a-f]{8}-[0-9a-f]{4}-[1-5][0-9a-f]{3}-[89ab][0-9a-f]{3}-[0-9a-f]{12}\\.json$"))
                        throw new Exception("S3 key '" + awsS3Key + "' is not an expected file name for transcribe output");

                    var transcribeJobUUID = awsS3Key.Substring(awsS3Key.IndexOf("-") + 1, awsS3Key.LastIndexOf("."));

                    var jobAssignmentId = StageVariables.PublicUrl + "/job-assignments/" + transcribeJobUUID;

                    var invokeParams = new InvokeRequest
                    {
                        FunctionName = StageVariables.WorkerLambdaFunctionName,
                        InvocationType = "Event",
                        LogType = "None",
                        Payload = new
                        {
                            action = "ProcessTranscribeJobResult",
                            stageVariables = StageVariables,
                            jobAssignmentId,
                            outputFile = new S3Locator { AwsS3Bucket = awsS3Bucket, AwsS3Key = awsS3Key }
                        }.ToMcmaJson().ToString()
                    };

                    var lambda = new AmazonLambdaClient();
                    await lambda.InvokeAsync(invokeParams);
                }
                catch (Exception error)
                {
                    Console.WriteLine($"Failed processing record.\r\nRecord:\r\n{record.ToMcmaJson()}\r\nError:\r\n{error}");
                }
            }
        }
    }
}