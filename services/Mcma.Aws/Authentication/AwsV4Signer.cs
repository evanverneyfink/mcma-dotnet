
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Mcma.Core.Utility;

namespace Mcma.Aws.Authentication
{
    public class AwsV4Signer : IAwsSigner
    {
        public AwsV4Signer(string accessKey, string secretKey, string region, string service = AwsConstants.Services.ExecuteApi)
        {
            AccessKey = accessKey;
            SecretKey = secretKey;
            Region = region;
            Service = service;
        }

        private string AccessKey { get; }

        private string SecretKey { get; }

        private string Region { get; }

        private string Service { get; }

        public async Task<HttpRequestMessage> SignAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            var awsDate = new AwsDate();

            request.Headers.Host = request.RequestUri.Host;
            request.Headers.Add(AwsConstants.Headers.Date, awsDate.DateTimeString);

            var stringToSign = StringToSign(awsDate, await request.ToHashedCanonicalRequestAsync());

            var signingKey = SigningKey(awsDate);

            var signature = signingKey.UseToSign(stringToSign).HexEncode();

            request.Headers.Authorization =
                new AuthenticationHeaderValue(
                    AwsConstants.Signing.Algorithm,
                    $"Credential={AccessKey}/{CredentialScope(awsDate)}, SignedHeaders={request.SignedHeaders()}, Signature={signature}");

            return request;
        }

        private string StringToSign(AwsDate awsDate, string hashedRequest) 
            =>
                AwsConstants.Signing.Algorithm + "\n" +
                awsDate.DateTimeString + "\n" +
                CredentialScope(awsDate) + "\n" +
                hashedRequest;

        private byte[] SigningKey(AwsDate awsDate)
            =>
                Encoding.UTF8.GetBytes(AwsConstants.Signing.SecretKeyPrefix + SecretKey)
                    .UseToSign(awsDate.DateString)
                    .UseToSign(Region)
                    .UseToSign(Service)
                    .UseToSign(AwsConstants.Signing.ScopeTerminator);

        private string CredentialScope(AwsDate awsDate)
            => $"{awsDate.DateString}/{Region}/{Service}/{AwsConstants.Signing.ScopeTerminator}";
    }
}