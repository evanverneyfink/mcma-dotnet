using System;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Amazon.Lambda.APIGatewayEvents;
using Amazon.Lambda.Core;
using Amazon.Lambda.Serialization.Json;
using Mcma.Aws;
using Mcma.Core.Serialization;

[assembly: LambdaSerializer(typeof(McmaLambdaSerializer))]

namespace Mcma.Aws.WorkflowService.ApiHandler
{
    public class Functions
    {
        private static ApiGatewayApiController Controller = new ApiGatewayApiController();

        static Functions()
        {
            Controller.AddRoute("GET", "/job-assignments", JobAssignmentRoutes.GetJobAssignmentsAsync);
            Controller.AddRoute("POST", "/job-assignments", JobAssignmentRoutes.AddJobAssignmentAsync);
            Controller.AddRoute("DELETE", "/job-assignments", JobAssignmentRoutes.DeleteJobAssignmentsAsync);
            Controller.AddRoute("GET", "/job-assignments/{id}", JobAssignmentRoutes.GetJobAssignmentAsync);
            Controller.AddRoute("DELETE", "/job-assignments/{id}", JobAssignmentRoutes.DeleteJobAssignmentAsync);

            Controller.AddRoute("POST", "/job-assignments/{id}/notifications", JobAssignmentRoutes.ProcessNotificationAsync);
        }

        public Task<APIGatewayProxyResponse> Handler(APIGatewayProxyRequest request, ILambdaContext context)
        {
            Console.WriteLine(request.ToMcmaJson().ToString());
            Console.WriteLine(context.ToMcmaJson().ToString());

            return Controller.HandleRequestAsync(request, context);
        }
    }
}
