﻿using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.Json;
using Mcma.Aws;
using Mcma.Core.Serialization;
using Mcma.Core.Logging;
using Mcma.Worker;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]
[assembly: McmaLambdaLogger]

namespace Mcma.Aws.AmeService.Worker
{
    public class Function
    {
        public async Task Handler(AmeServiceWorkerRequest @event, ILambdaContext context)
        {
            Logger.Debug(@event.ToMcmaJson().ToString());
            Logger.Debug(context.ToMcmaJson().ToString());

            await McmaWorker.DoWorkAsync<AmeServiceWorker, AmeServiceWorkerRequest>(@event.Action, @event);
        }
    }
}
