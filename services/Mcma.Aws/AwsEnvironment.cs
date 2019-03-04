using System;
using System.Collections.Generic;
using Mcma.Api;
using Mcma.Aws.Authentication;
using Mcma.Core;
using Mcma.Core.Serialization;

namespace Mcma.Aws
{
    public static class AwsEnvironment
    {
        public static string ServicesUrl => Environment.GetEnvironmentVariable("SERVICES_URL");

        public static string ServicesAuthType => Environment.GetEnvironmentVariable("SERVICES_AUTH_TYPE");
        
        public static string ServicesAuthContext => Environment.GetEnvironmentVariable("SERVICES_AUTH_CONTEXT");

        public static string AccessKey => Environment.GetEnvironmentVariable("AWS_ACCESS_KEY_ID");

        public static string SecretKey => Environment.GetEnvironmentVariable("AWS_SECRET_ACCESS_KEY ");

        public static string SessionToken => Environment.GetEnvironmentVariable("AWS_SESSION_TOKEN");

        public static string Region => Environment.GetEnvironmentVariable("AWS_REGION") ?? Environment.GetEnvironmentVariable("AWS_DEFAULT_REGION");

        public static AwsV4AuthContext GetAwsV4DefaultAuthContext()
            => new AwsV4AuthContext
                {
                    AccessKey = AwsEnvironment.AccessKey,
                    SecretKey = AwsEnvironment.SecretKey,
                    SessionToken = AwsEnvironment.SessionToken,
                    Region = AwsEnvironment.Region
                };

        public static IMcmaAuthenticatorProvider GetAwsV4AuthProvider()
            => new AwsAuthenticatorProvider(
                new Dictionary<string, string>
                {
                    [AwsConstants.AWS4] = GetAwsV4DefaultAuthContext().ToMcmaJson().ToString()
                });

        public static ResourceManagerOptions GetAwsV4ResourceManagerOptions()
            => new ResourceManagerOptions(ServicesUrl)
                .WithAuth(
                    GetAwsV4AuthProvider(),
                    ServicesAuthType,
                    ServicesAuthContext
                );

        public static ResourceManager GetAwsV4ResourceManager()
            => new ResourceManager(GetAwsV4ResourceManagerOptions());

        public static ResourceManagerOptions GetAwsV4ResourceManagerOptions(this McmaApiRequest request)
            => new ResourceManagerOptions(request.StageVariables["ServicesUrl"])
                .WithAuth(
                    GetAwsV4AuthProvider(),
                    request.StageVariables.ContainsKey("ServicesAuthType") ? request.StageVariables["ServicesAuthType"] : null,
                    request.StageVariables.ContainsKey("ServicesAuthContext") ? request.StageVariables["ServicesAuthContext"] : null
                );

        public static ResourceManager GetAwsV4ResourceManager(this McmaApiRequest request)
            => new ResourceManager(request.GetAwsV4ResourceManagerOptions());
    }
}