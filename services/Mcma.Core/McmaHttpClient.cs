using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Mcma.Core
{

    public class McmaHttpClient
    {
        public McmaHttpClient(IMcmaAuthenticator authenticator = null)
        {
            HttpClient = authenticator?.CreateAuthenticatedClient() ?? new HttpClient();
        }

        private HttpClient HttpClient { get; }

        public Task<HttpResponseMessage> GetAsync(string url, IDictionary<string, string> headers = null)
            => SendAsync(url, HttpMethod.Get, headers, null);

        public Task<HttpResponseMessage> PostAsync(string url, HttpContent body, IDictionary<string, string> headers = null)
            => SendAsync(url, HttpMethod.Post, headers, body);

        public Task<HttpResponseMessage> PutAsync(string url, HttpContent body, IDictionary<string, string> headers = null)
            => SendAsync(url, HttpMethod.Put, headers, body);

        public Task<HttpResponseMessage> DeleteAsync(string url, IDictionary<string, string> headers = null)
            => SendAsync(url, HttpMethod.Delete, headers, null);

        private Task<HttpResponseMessage> SendAsync(string url, HttpMethod method, IDictionary<string, string> headers, HttpContent body)
        {
            var request = new HttpRequestMessage(method, url);
            
            if (headers != null)
                foreach (var header in headers)
                    request.Headers.Add(header.Key, header.Value);

            if (body != null)
                request.Content = body;

            return HttpClient.SendAsync(request);
        }
    }
}