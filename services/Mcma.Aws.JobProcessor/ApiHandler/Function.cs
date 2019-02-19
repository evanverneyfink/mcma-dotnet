﻿using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.Json;
using Mcma.Core.Serialization;
using Mcma.Aws;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]

namespace Mcma.Aws.JobProcessor.ApiHandler
{
    public class Function
    {
        private static ApiGatewayApiController Controller = new ApiGatewayApiController();

        static Function()
        {
            Controller.AddRoute("GET", "/job-processes", JobProcessRoutes.GetJobProcessesAsync);
            Controller.AddRoute("POST", "/job-processes", JobProcessRoutes.AddJobProcessAsync);
            Controller.AddRoute("GET", "/job-processes/{id}", JobProcessRoutes.GetJobProcessAsync);
            Controller.AddRoute("DELETE", "/job-processes/{id}", JobProcessRoutes.DeleteJobProcessAsync);

            Controller.AddRoute("POST", "/job-processes/{id}/notifications", JobProcessRoutes.ProcessNotificationAsync);
        }

        public Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            Console.WriteLine(request.ToMcmaJson().ToString());
            Console.WriteLine(context.ToMcmaJson().ToString());

            return Controller.HandleRequestAsync(request, context);
        }
    }
}
