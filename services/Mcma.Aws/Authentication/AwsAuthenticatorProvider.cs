using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Mcma.Core;
using Mcma.Core.Serialization;
using Newtonsoft.Json.Linq;

namespace Mcma.Aws.Authentication
{
    public class AwsAuthenticatorProvider : IMcmaAuthenticatorProvider
    {
        public AwsAuthenticatorProvider(IDictionary<string, string> defaultAuthContexts)
        {
            DefaultAuthContexts = defaultAuthContexts ?? new Dictionary<string, string>();
        }

        private IDictionary<string, string> DefaultAuthContexts { get; }

        public Task<IMcmaAuthenticator> GetAuthenticatorAsync(string authType, string authContext)
        {
            switch (authType)
            {
                case AwsConstants.AWSV4:
                    return Task.FromResult<IMcmaAuthenticator>(new AwsV4Authenticator(GetAuthContext<AwsV4AuthContext>(authType, authContext, true)));
                default:
                    throw new Exception($"Unrecognized authentication type '{authType}'");
            }
        }

        private T GetAuthContext<T>(string authType, string authContext, bool required)
        {
            if (string.IsNullOrWhiteSpace(authContext) && DefaultAuthContexts.ContainsKey(authType))
                authContext = DefaultAuthContexts[authType];

            if (string.IsNullOrWhiteSpace(authContext) && required)
                throw new Exception($"Auth type {authType} requires a context.");

            try
            {
                return JObject.Parse(authContext).ToMcmaObject<T>();
            }
            catch (Exception ex)
            {
                throw new Exception($"Failed to parse an auth context object of type {typeof(T).Name} from JSON '{authContext}'.", ex);
            }
        }
    }
}