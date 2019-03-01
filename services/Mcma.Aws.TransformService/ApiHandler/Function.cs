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

namespace Mcma.Aws.TransformService.ApiHandler
{
    public class Function
    {
        private static ApiGatewayApiController Controller = new ApiGatewayApiController();

        static Function()
        {
            Controller.AddRoute("GET", "/job-assignments", JobAssignmentRoutes.GetJobAssignmentsAsync);
            Controller.AddRoute("POST", "/job-assignments", JobAssignmentRoutes.AddJobAssignmentAsync);
            Controller.AddRoute("DELETE", "/job-assignments", JobAssignmentRoutes.DeleteJobAssignmentsAsync);
            Controller.AddRoute("GET", "/job-assignments/{id}", JobAssignmentRoutes.GetJobAssignmentAsync);
            Controller.AddRoute("DELETE", "/job-assignments/{id}", JobAssignmentRoutes.DeleteJobAssignmentAsync);

            // adding route for notifications from ec2 transform service
            Controller.AddRoute("POST", "/job-assignments/{id}/notifications", JobAssignmentRoutes.ProcessNotificationAsync);
        }

        public Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            Logger.Debug(request.ToMcmaJson().ToString());
            Logger.Debug(context.ToMcmaJson().ToString());

            return Controller.HandleRequestAsync(request, context);
        }
    }
}
