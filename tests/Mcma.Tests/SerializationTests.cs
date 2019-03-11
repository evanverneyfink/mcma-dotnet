using System;
using Newtonsoft.Json.Linq;
using Mcma.Core;
using Mcma.Core.Serialization;

namespace Mcma.Tests
{
    public static class SerializationTests
    {
        public static void ToMcmaObject_ShouldDeserializeWorkflowJob()
        {
            var workflowJobJson =
            @"{
                ""@type"": ""WorkflowJob"",
                ""jobProfile"": ""https://w7eijekmuf.execute-api.us-east-2.amazonaws.com/dev/job-profiles/89f432b2-8e90-4d64-9d5d-9c082d6c3574"",
                ""jobInput"": {
                    ""@type"": ""JobParameterBag"",
                    ""metadata"": {
                        ""name"": ""test 1"",
                        ""description"": ""test 1""
                    },
                    ""inputFile"": {
                        ""@type"": ""S3Locator"",
                        ""awsS3Bucket"": ""triskel.mcma.us-east-2.dev.upload"",
                        ""awsS3Key"": ""ShowbizPKG091218__091118178.mp4""
                    }
                }
            }";

            var workflowJob = JObject.Parse(workflowJobJson).ToMcmaObject<WorkflowJob>();

            var serialized = workflowJob.ToMcmaJson();
            
            Console.WriteLine(serialized);
        }
    }
}
