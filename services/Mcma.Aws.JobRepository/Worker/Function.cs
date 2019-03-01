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

namespace Mcma.Aws.JobRepository.Worker
{
    public class Function
    {
        public async Task Handler(JobRepositoryWorkerRequest @event, ILambdaContext context)
        {
            Logger.Debug(@event.ToMcmaJson().ToString());
            Logger.Debug(context.ToMcmaJson().ToString());

            switch (@event.Action)
            {
                case "createJobProcess":
                    await JobRepositoryWorker.CreateJobProcessAsync(@event);
                    break;
                case "deleteJobProcess":
                    await JobRepositoryWorker.DeleteJobProcessAsync(@event);
                    break;
                case "processNotification":
                    await JobRepositoryWorker.ProcessNotificationAsync(@event);
                    break;
                default:
                    Console.Error.WriteLine("No handler implemented for action '" + @event.Action + "'.");
                    break;
            }
        }
    }
}
