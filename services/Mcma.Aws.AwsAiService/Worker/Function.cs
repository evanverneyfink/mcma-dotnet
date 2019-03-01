using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.Json;
using Mcma.Aws;
using Mcma.Core.Serialization;
using Mcma.Core.Logging;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]

namespace Mcma.Aws.AwsAiService.Worker
{
    public class Function
    {
        public async Task Handler(AwsAiServiceWorkerRequest @event, ILambdaContext context)
        {
            Logger.Debug(@event.ToMcmaJson().ToString());
            Logger.Debug(context.ToMcmaJson().ToString());

            switch (@event.Action)
            {
                case "ProcessJobAssignment":
                    await AwsAiServiceWorker.ProcessJobAssignmentAsync(@event);
                    break;
                case "ProcessTranscribeResult":
                    await AwsAiServiceWorker.ProcessTranscribeResultAsync(@event);
                    break;
                case "ProcessRekognitionResult":
                    await AwsAiServiceWorker.ProcessRekognitionResultAsync(@event);
                    break;
                default:
                    Console.Error.WriteLine("No handler implemented for action '" + @event.Action + "'.");
                    break;
            }
        }
    }
}
