using Synchron.Core.Models;

namespace Synchron.Core.Interfaces;

public interface ITaskListExecutor
{
    Task<TaskListResult> ExecuteAsync(TaskListConfig config, CancellationToken cancellationToken = default);
    Task<TaskResult> ExecuteTaskAsync(SyncTask task, CancellationToken cancellationToken = default);
}

public class TaskListResult
{
    public bool Success { get; set; }
    public List<TaskResult> TaskResults { get; set; } = new();
    public TimeSpan TotalDuration { get; set; }
    public int TasksCompleted { get; set; }
    public int TasksFailed { get; set; }
    public int TasksSkipped { get; set; }
    public long TotalBytesTransferred { get; set; }
    public List<string> Errors { get; set; } = new();

    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }
}

public class TaskResult
{
    public string TaskName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public bool Skipped { get; set; }
    public SyncResult? SyncResult { get; set; }
    public string? ErrorMessage { get; set; }
    public TimeSpan Duration { get; set; }

    public override string ToString()
    {
        if (Skipped) return $"[{TaskName}] Skipped";
        return Success 
            ? $"[{TaskName}] Success" 
            : $"[{TaskName}] Failed: {ErrorMessage}";
    }
}
