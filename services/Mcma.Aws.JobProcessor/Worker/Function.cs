using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.Json;
using Mcma.Core.Serialization;
using Mcma.Aws;
using Mcma.Core.Logging;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]

namespace Mcma.Aws.JobProcessor.Worker
{
    public class Function
    {
        public async Task Handler(JobProcessorWorkerRequest @event, ILambdaContext context)
        {
            Logger.Debug(@event.ToMcmaJson().ToString());
            Logger.Debug(context.ToMcmaJson().ToString());

            switch (@event.Action)
            {
                case "createJobAssignment":
                    await JobProcessorWorker.CreateJobAssignmentAsync(@event);
                    break;
                case "deleteJobAssignment":
                    await JobProcessorWorker.DeleteJobAssignmentAsync(@event);
                    break;
                case "processNotification":
                    await JobProcessorWorker.ProcessNotificationAsync(@event);
                    break;
                default:
                    Console.Error.WriteLine("No handler implemented for action '" + @event.Action + "'.");
                    break;
            }
        }
    }
}
