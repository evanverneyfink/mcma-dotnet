﻿using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.Json;
using Mcma.Aws;
using Mcma.Core.Serialization;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]

namespace Mcma.Aws.TransformService.Worker
{
    public class Function
    {
        public async Task Handler(TransformServiceWorkerRequest @event, ILambdaContext context)
        {
            Console.WriteLine(@event.ToMcmaJson().ToString());
            Console.WriteLine(context.ToMcmaJson().ToString());

            switch (@event.Action)
            {
                case "ProcessJobAssignment":
                    await TransformServiceWorker.ProcessJobAssignmentAsync(@event);
                    break;
                case "ProcessNotification":
                    await TransformServiceWorker.ProcessNotificationAsync(@event);
                    break;
                default:
                    Console.Error.WriteLine("No handler implemented for action '" + @event.Action + "'.");
                    break;
            }
        }
    }
}
