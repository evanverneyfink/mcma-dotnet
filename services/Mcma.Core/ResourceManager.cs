using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Mcma.Core.Serialization;

namespace Mcma.Core
{
    public class ResourceManager
    {
        public ResourceManager(string servicesUrl, IMcmaAuthenticator authenticator = null)
        {
            ServicesUrl = servicesUrl;
            HttpClient = new McmaHttpClient(authenticator);
        }

        private string ServicesUrl { get; }

        private List<Service> Services { get; } = new List<Service>();

        private McmaHttpClient HttpClient { get; }

        private Service DefaultServiceRegistryService =>
            new Service
            {
                Name = "Service Registry",
                Resources = new List<ServiceResource>
                {
                    new ServiceResource
                    {
                        ResourceType = nameof(Service),
                        HttpEndpoint = ServicesUrl
                    }
                }
            };

        public async Task InitAsync()
        {
            var response = (await HttpClient.GetAsync(ServicesUrl)).EnsureSuccessStatusCode();

            Services.AddRange(await response.Content.ReadAsArrayFromJsonAsync<Service>());

            var serviceRegistryPresent = 
                Services.SelectMany(svc => svc.Resources ?? new ServiceResource[0])
                    .Any(svcRes => svcRes.ResourceType == nameof(Service));

            if (!serviceRegistryPresent)
                Services.Add(DefaultServiceRegistryService);
        }

        private IEnumerable<string> GetUrls<T>((string, string)[] filter = null) 
        {
            var serviceResources =
                Services.SelectMany(svc => svc.Resources ?? new ServiceResource[0])
                    .Where(svcRes => typeof(T).Name == svcRes.ResourceType);

            foreach (var serviceResource in serviceResources)
            {
                var uriBuilder = new UriBuilder(serviceResource.HttpEndpoint);
                
                if (filter != null)
                    uriBuilder.Query = string.Join("&", filter.Select(x => $"{x.Item1}={x.Item2}"));

                yield return uriBuilder.Uri.ToString();
            }
        }

        public async Task<IEnumerable<T>> GetAsync<T>(params (string, string)[] filter)
        {
            if (!Services.Any())
                await InitAsync();

            var results = new List<T>();

            foreach (var serviceResourceUrl in GetUrls<T>(filter))
            {
                try
                {
                    var response = (await HttpClient.GetAsync(serviceResourceUrl)).EnsureSuccessStatusCode();

                    results.AddRange(await response.Content.ReadAsArrayFromJsonAsync<T>());
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to retrieve '{typeof(T).Name}' from endpoint '{serviceResourceUrl}'.");
                    Console.WriteLine(ex);
                }
            }

            return new ReadOnlyCollection<T>(results);
        }

        public async Task<T> CreateAsync<T>(T resource)
        {
            if (!Services.Any())
                await InitAsync();

            
            foreach (var serviceResourceUrl in GetUrls<T>())
            {
                try
                {
                    var response = (await HttpClient.PostAsJsonAsync(serviceResourceUrl, resource)).EnsureSuccessStatusCode();

                    return await response.Content.ReadAsObjectFromJsonAsync<T>();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Failed to retrieve '{typeof(T).Name}' from endpoint '{serviceResourceUrl}'.");
                    Console.WriteLine(ex);
                }
            }

            throw new Exception($"Failed to find service to create resource of type '{typeof(T).Name}'." );
        }

        public async Task<T> UpdateAsync<T>(T resource) where T : IMcmaResource
        {
            var response = (await HttpClient.PutAsJsonAsync(resource.Id, resource)).EnsureSuccessStatusCode();

            return await response.Content.ReadAsObjectFromJsonAsync<T>();
        }

        public async Task DeleteAsync(IMcmaResource resource)
            => (await HttpClient.DeleteAsync(resource.Id)).EnsureSuccessStatusCode();

        public async Task SendNotificationAsync<T>(T resource, string notificationEndpoint) where T : IMcmaResource
        {
            if (!string.IsNullOrWhiteSpace(notificationEndpoint))
            {
                var resp = (await HttpClient.GetAsync(notificationEndpoint)).EnsureSuccessStatusCode();

                await SendNotificationAsync(resource, await resp.Content.ReadAsObjectFromJsonAsync<NotificationEndpoint>());
            }
        }

        public async Task SendNotificationAsync<T>(T resource, NotificationEndpoint notificationEndpoint) where T : IMcmaResource
        {
            if (!string.IsNullOrWhiteSpace(notificationEndpoint?.HttpEndpoint))
            {
                var notification = new Notification{Source = resource.Id, Content = resource.ToMcmaJson()};

                (await HttpClient.PostAsJsonAsync(notificationEndpoint.HttpEndpoint, notification)).EnsureSuccessStatusCode();
            }
        }
    }
}