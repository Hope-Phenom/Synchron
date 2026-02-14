namespace Synchron.Core.Models;

public class TaskListConfig
{
    public string? Name { get; set; }
    public List<SyncTask> Tasks { get; set; } = new();
    public bool StopOnError { get; set; } = true;
    public int MaxParallelTasks { get; set; } = 1;

    public int EnabledTaskCount => Tasks.Count(t => t.Enabled);
    public int TotalTaskCount => Tasks.Count;

    public override string ToString()
    {
        var name = string.IsNullOrEmpty(Name) ? "Task List" : Name;
        return $"{name}: {EnabledTaskCount}/{TotalTaskCount} tasks enabled";
    }
}
