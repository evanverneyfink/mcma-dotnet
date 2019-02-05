#load "cmd-task.csx"

public class DotNetCli : CmdTask
{
    public DotNetCli(string project, string command, params string[] additionalArgs)
        : base("dotnet", string.Join(" ", new[] {command, project, string.Join(" ", additionalArgs)}))
    {
    }

    public static DotNetCli Clean(string project, string outputDir, string config = "Release")
        => new DotNetCli(project, "clean", $"-o={outputDir}", $"-c={config}");

    public static DotNetCli Restore(string project)
        => new DotNetCli(project, "restore");

    public static DotNetCli Build(string project, string outputDir, string config = "Release")
        => new DotNetCli(project, "build", $"-o={outputDir}", $"-c={config}");

    public static DotNetCli Publish(string project, string outputDir, string config = "Release")
        => new DotNetCli(project, "publish", $"-o={outputDir}", $"-c={config}");
}