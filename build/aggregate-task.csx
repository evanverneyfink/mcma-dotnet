#load "task.csx"

public class AggregateTask : IBuildTask
{
    public AggregateTask(params IBuildTask[] tasks)
    {
        Tasks = new List<IBuildTask>(tasks);
    }

    private List<IBuildTask> Tasks { get; }

    public AggregateTask Add(IBuildTask task)
    {
        Tasks.Add(task);
        return this;
    }

    public async Task<bool> Run()
    {
        foreach (var task in Tasks)
            if (!await task.Run())
                break;
        
        return true;
    }
}