#load "task.csx"
#load "cmd-task.csx"

public static class Npm
{
    public static string Path { get; set; } = "C:\\Program Files\\nodejs\\npm.cmd";

    public static IBuildTask Install(string dir) => new CmdTask(Path, "install") {Cwd = dir};

    public static IBuildTask RunScript(string dir, string script, params string[] args)
        => new CmdTask(Path, new[] {"run-script", script, "--"}.Concat(args).ToArray()) {Cwd = dir};
}