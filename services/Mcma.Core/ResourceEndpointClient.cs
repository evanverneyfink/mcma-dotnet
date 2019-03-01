using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Threading.Tasks;

namespace Mcma.Core
{
    public class ResourceEndpointClient
    {
        public ResourceEndpointClient(ResourceEndpoint resourceEndpoint, IMcmaAuthenticatorProvider authProvider = null, string authType = null, string authContext = null)
        {
            Data = resourceEndpoint;

            HttpClientTask =
                new Lazy<Task<McmaHttpClient>>(async () =>
                    new McmaHttpClient(
                        authProvider != null
                            ? await authProvider.GetAuthenticatorAsync(authType ?? resourceEndpoint.AuthType, authContext ?? resourceEndpoint.AuthContext)
                            : null,
                        resourceEndpoint.HttpEndpoint));
        }

        public ResourceEndpoint Data { get; }

        private Lazy<Task<McmaHttpClient>> HttpClientTask { get; }

        private async Task<HttpResponseMessage> ExecuteAsync(Func<McmaHttpClient, Task<HttpResponseMessage>> execute)
            => await execute(await HttpClientTask.Value);

        private async Task<T> ExecuteObjectAsync<T>(Func<McmaHttpClient, Task<HttpResponseMessage>> execute)
        {
            var response = await ExecuteAsync(execute);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsObjectFromJsonAsync<T>();
        }

        private async Task<T[]> ExecuteCollectionAsync<T>(Func<McmaHttpClient, Task<HttpResponseMessage>> execute, bool throwIfAnyFailToDeserialize)
        {
            var response = await ExecuteAsync(execute);
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadAsArrayFromJsonAsync<T>(throwIfAnyFailToDeserialize);
        }

        public async Task<HttpResponseMessage> GetAsync(string url = null)
            => await ExecuteAsync(async httpClient => await httpClient.GetAsync(url));

        public async Task<T> GetAsync<T>(string url = null)
            => await ExecuteObjectAsync<T>(async httpClient => await httpClient.GetAsync(url));

        public async Task<IEnumerable<T>> GetCollectionAsync<T>(string url = null, IDictionary<string, string> filter = null, bool throwIfAnyFailToDeserialize = true)
            => await ExecuteCollectionAsync<T>(async httpClient => await httpClient.GetAsync(url, filter), throwIfAnyFailToDeserialize);

        public async Task<HttpResponseMessage> PostAsync(object body, string url = null)
            => await ExecuteAsync(async httpClient => await httpClient.PostAsJsonAsync(url, body));

        public async Task<T> PostAsync<T>(T body, string url = null)
            => await ExecuteObjectAsync<T>(async httpClient => await httpClient.PostAsJsonAsync(url, body));

        public async Task<HttpResponseMessage> PutAsync(object body, string url = null)
            => await ExecuteAsync(async httpClient => await httpClient.PutAsJsonAsync(url, body));

        public async Task<T> PutAsync<T>(T body, string url = null)
            => await ExecuteObjectAsync<T>(async httpClient => await httpClient.PutAsJsonAsync(url, body));

        public async Task<HttpResponseMessage> PatchAsync(object body, string url = null)
            => await ExecuteAsync(async httpClient => await httpClient.PatchAsJsonAsync(url, body));

        public async Task<T> PatchAsync<T>(T body, string url = null)
            => await ExecuteObjectAsync<T>(async httpClient => await httpClient.PatchAsJsonAsync(url, body));

        public async Task<HttpResponseMessage> DeleteAsync(string url = null)
            => await ExecuteAsync(async httpClient => await httpClient.DeleteAsync(url));
    }
}