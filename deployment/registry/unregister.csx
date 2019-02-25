#load "../../build/build.csx"
#load "../../build/task.csx"

#r "nuget:Newtonsoft.Json, 11.0.2"
#r "../../services/Mcma.Core/bin/Debug/netstandard2.0/Mcma.Core.dll"

using System.Threading.Tasks;
using Mcma.Core;

public class ClearServiceRegistry : BuildTask
{
    private IDictionary<string, string> ParseContent(string content)
    {
        var serviceUrls = new Dictionary<string, string>();

        foreach (var line in content.Split('\n'))
        {
            var parts = line.Split(new[] {" = "}, StringSplitOptions.RemoveEmptyEntries);

            if (parts.Length == 2)
                serviceUrls[parts[0]] = parts[1].Trim();
        }

        return serviceUrls;
    }

    protected override async Task<bool> ExecuteTask()
    {
        var content = File.ReadAllText($"{Build.Dirs.Deployment.TrimEnd('/')}/terraform.output");
        var terraformOutput = ParseContent(content);
        
        var servicesUrl = $"{terraformOutput["service_registry_url"]}/services";

        var resourceManager = new ResourceManager(servicesUrl);
        await resourceManager.InitAsync();

        foreach (var jobProfile in await resourceManager.GetAsync<JobProfile>())
            await resourceManager.DeleteAsync(jobProfile);
        
        foreach (var service in  await resourceManager.GetAsync<Service>())
            await resourceManager.DeleteAsync(service);

        return true;
    }
}