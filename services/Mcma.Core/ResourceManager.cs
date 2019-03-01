using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;
using Mcma.Core.Serialization;
using Mcma.Core.Logging;

namespace Mcma.Core
{
    public class ResourceManager
    {
        public ResourceManager(ResourceManagerOptions options)
        {
            if (options == null) throw new ArgumentNullException(nameof(options));

            Options = options;
        }

        private ResourceManagerOptions Options { get; }

        private List<ServiceClient> Services { get; } = new List<ServiceClient>();

        private McmaHttpClient HttpClient { get; } = new McmaHttpClient();

        private ServiceClient GetDefaultServiceRegistryServiceClient() =>
            new ServiceClient(
                new Service
                {
                    Name = "Service Registry",
                    Resources = new List<ResourceEndpoint>
                    {
                        new ResourceEndpoint
                        {
                            ResourceType = nameof(Service),
                            HttpEndpoint = Options.ServicesUrl,
                            AuthType = Options.ServicesAuthType,
                            AuthContext = Options.ServicesAuthContext
                        }
                    }
                },
                Options.AuthProvider
            );

        public async Task InitAsync()
        {
            try
            {
                Services.Clear();

                var serviceRegistry = GetDefaultServiceRegistryServiceClient();
                Services.Add(serviceRegistry);

                var servicesEndpoint = serviceRegistry.GetResourceEndpoint<Service>();

                Logger.Debug($"Retrieving services from {Options.ServicesUrl}...");

                var response = await servicesEndpoint.GetCollectionAsync<Service>(throwIfAnyFailToDeserialize: false);

                Services.AddRange(response.Select(svc => new ServiceClient(svc, Options.AuthProvider)));
            }
            catch (Exception error)
            {
                throw new Exception("ResourceManager: Failed to initialize", error);
            }
        }

        public async Task<IEnumerable<T>> GetAsync<T>(params (string, string)[] filter)
        {
            if (!Services.Any())
                await InitAsync();

            var results = new List<T>();
            var usedHttpEndpoints = new Dictionary<string, bool>();

            foreach (var resourceEndpoint in Services.Where(s => s.HasResourceEndpoint<T>()).Select(s => s.GetResourceEndpoint<T>()))
            {
                try
                {
                    if (!usedHttpEndpoints.ContainsKey(resourceEndpoint.Data.HttpEndpoint))
                    {
                        var response = await resourceEndpoint.GetCollectionAsync<T>(filter: filter.ToDictionary(x => x.Item1, x => x.Item2));
                        results.AddRange(response);
                    }

                    usedHttpEndpoints[resourceEndpoint.Data.HttpEndpoint] = true;
                }
                catch (Exception error)
                {
                    Logger.Error("Failed to retrieve '" + typeof(T).Name + "' from endpoint '" + resourceEndpoint.Data.HttpEndpoint + "'");
                    Logger.Exception(error);
                }
            }

            return new ReadOnlyCollection<T>(results);
        }

        public async Task<T> CreateAsync<T>(T resource)
        {
            if (!Services.Any())
                await InitAsync();

            var resourceEndpoint = Services.Where(s => s.HasResourceEndpoint<T>()).Select(s => s.GetResourceEndpoint<T>()).FirstOrDefault();
            if (resourceEndpoint != null)
                return await resourceEndpoint.PostAsync<T>(resource);

            throw new Exception("ResourceManager: Failed to find service to create resource of type '" + typeof(T).Name + "'.");
        }

        public async Task<T> UpdateAsync<T>(T resource) where T : IMcmaResource
        {
            if (!Services.Any())
                await InitAsync();

            var resourceEndpoint =
                Services.Where(s => s.HasResourceEndpoint<T>())
                    .Select(s => s.GetResourceEndpoint<T>())
                    .FirstOrDefault(re => resource.Id.StartsWith(re.Data.HttpEndpoint, StringComparison.OrdinalIgnoreCase));
            if (resourceEndpoint != null)
                return await resourceEndpoint.PostAsync<T>(resource);

            var resp = await HttpClient.PutAsJsonAsync(resource.Id, resource);
            return await resp.Content.ReadAsObjectFromJsonAsync<T>();
        }

        public async Task DeleteAsync(IMcmaResource resource)
        {
            if (!Services.Any())
                await InitAsync();

            var resourceEndpoint =
                Services.Where(s => s.HasResourceEndpoint(resource.Type))
                    .Select(s => s.GetResourceEndpoint(resource.Type))
                    .FirstOrDefault(re => resource.Id.StartsWith(re.Data.HttpEndpoint, StringComparison.OrdinalIgnoreCase));
            if (resourceEndpoint != null)
                await resourceEndpoint.DeleteAsync(resource.Id);
            else
                await HttpClient.DeleteAsync(resource.Id);
        }

        public async Task<ResourceEndpointClient> GetResourceEndpointAsync(string url)
        {
            if (!Services.Any())
                await InitAsync();

            return Services.SelectMany(s => s.Resources)
                .FirstOrDefault(re => re.Data.HttpEndpoint.StartsWith(url, StringComparison.OrdinalIgnoreCase));
        }

        public async Task SendNotificationAsync<T>(T resource, NotificationEndpoint notificationEndpoint) where T : IMcmaResource
        {
            if (!string.IsNullOrWhiteSpace(notificationEndpoint?.HttpEndpoint))
            {
                var notification = new Notification {Source = resource.Id, Content = resource.ToMcmaJson()};

                var resourceEndpoint = await GetResourceEndpointAsync(notificationEndpoint.HttpEndpoint);

                if (resourceEndpoint != null)
                    await resourceEndpoint.PostAsync(notification, notificationEndpoint.HttpEndpoint);
                else
                    (await HttpClient.PostAsJsonAsync(notificationEndpoint.HttpEndpoint, notification)).EnsureSuccessStatusCode();
            }
        }
    }
}