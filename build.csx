#load "build/task.csx"
#load "build/build.csx"
#load "build/aggregate-task.csx"

#load "services/build-tasks.csx"
#load "workflows/build-tasks.csx"
#load "website/build-tasks.csx"
#load "deployment/build-tasks.csx"
#load "deployment/registry/register.csx"
#load "deployment/registry/unregister.csx"

Build.Dirs.Deployment = Terraform.DefaultProjectDir = $"{Build.RootDir.TrimEnd('/')}/deployment";

Build.ReadInputs(defaults: new Dictionary<string, string>
{
    ["awsInstanceType"] = "t2.micro",
    ["awsInstanceCount"] = "1",
    ["AzureLocation"] = "trial",
    ["AzureAccountID"] = "undefined",
    ["AzureSubscriptionKey"] = "undefined",
    ["AzureApiUrl"] = "https://api.videoindexer.ai"
});

public static readonly IBuildTask BuildAll = new AggregateTask(BuildServices, BuildWorkflows, BuildWebsite);

Build.Tasks["buildServices"] = BuildServices;
Build.Tasks["buildWorkflows"] = BuildWorkflows;
Build.Tasks["buildWebsite"] = BuildWebsite;
Build.Tasks["build"] = BuildAll;
Build.Tasks["deployNoBuild"] = Deploy;
Build.Tasks["deploy"] = new AggregateTask(BuildAll, Deploy);
Build.Tasks["destroy"] = Destroy;
Build.Tasks["register"] = new UpdateServiceRegistry();
Build.Tasks["unregister"] = new ClearServiceRegistry();
Build.Tasks["tfOutput"] = new RetrieveTerraformOutput();
Build.Tasks["generateWebsiteTf"] = new GenerateTerraformWebsiteTf();

await Build.Run(Args?.FirstOrDefault());