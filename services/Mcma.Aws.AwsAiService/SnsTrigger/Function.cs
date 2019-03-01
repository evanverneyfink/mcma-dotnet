using System;
using System.Threading.Tasks;
using Amazon.Lambda;
using Amazon.Lambda.Core;
using Amazon.Lambda.Model;
using Amazon.Lambda.Serialization;
using Amazon.Lambda.SNSEvents;
using Newtonsoft.Json.Linq;
using Mcma.Aws;
using Mcma.Core.Serialization;
using System.Text;
using Mcma.Core.Logging;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]

namespace Mcma.Aws.AwsAiService.SnsTrigger
{

    public class Function
    {
        private StageVariables StageVariables { get; } = new StageVariables();

        public async Task Handler(SNSEvent @event, ILambdaContext context)
        {
            if (@event == null || @event.Records == null)
                return;

            foreach (var record in @event.Records)
            {
                try
                {
                    if (record.Sns == null)
                        throw new Exception("The payload doesn't contain expected data: Sns");

                    if (record.Sns.Message == null)
                        throw new Exception("The payload doesn't contain expectd data: Sns.Message");

                    dynamic message = JToken.Parse(record.Sns.Message);
                    Logger.Debug($"SNS Message ==> {message}");

                    var rekoJobId = message.JobId;
                    var rekoJobType = message.API;
                    var status = message.Status;

                    var jt = message.JobTag.ToString();
                    if (jt == null)
                        throw new Exception($"The jobAssignment couldn't be found in the SNS message");

                    var jobAssignmentId = Encoding.UTF8.GetString(BitConverter.GetBytes(jt));

                    Logger.Debug($"rekoJobId: {rekoJobId}");
                    Logger.Debug($"rekoJobType: {rekoJobType}");
                    Logger.Debug($"status: {status}");
                    Logger.Debug($"jobAssignmentId: {jobAssignmentId}");

                    var invokeParams = new InvokeRequest
                    {
                        FunctionName = StageVariables.WorkerLambdaFunctionName,
                        InvocationType = "Event",
                        LogType = "None",
                        Payload = JObject.FromObject(new
                        {
                            action = "ProcessRekognitionResult",
                            stageVariables = StageVariables,
                            jobAssignmentId,
                            jobExternalInfo = new
                            {
                                rekoJobId,
                                rekoJobType,
                                status
                            }
                        }).ToString()
                    };

                    var lambda = new AmazonLambdaClient();
                    await lambda.InvokeAsync(invokeParams);
                }
                catch (Exception error)
                {
                    Logger.Error($"Failed processing record.\r\nRecord:\r\n{record.ToMcmaJson()}\r\nError:\r\n{error}");
                }
            }
        }
    }
}