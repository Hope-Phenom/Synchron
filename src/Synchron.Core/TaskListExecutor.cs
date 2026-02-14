using System.Diagnostics;
using Synchron.Core.Interfaces;
using Synchron.Core.Models;

namespace Synchron.Core;

public sealed class TaskListExecutor : ITaskListExecutor, IDisposable
{
    private readonly ILogger _logger;
    private readonly Func<ILogger, IFileFilter, SyncEngine> _syncEngineFactory;
    private readonly object _lock = new();
    private bool _disposed;

    public event EventHandler<TaskProgressEventArgs>? TaskProgress;

    public TaskListExecutor(ILogger logger)
    {
        _logger = logger;
        _syncEngineFactory = (log, filter) => new SyncEngine(log, filter, new ConfigManager(log));
    }

    internal TaskListExecutor(ILogger logger, Func<ILogger, IFileFilter, SyncEngine> syncEngineFactory)
    {
        _logger = logger;
        _syncEngineFactory = syncEngineFactory;
    }

    public async Task<TaskListResult> ExecuteAsync(TaskListConfig config, CancellationToken cancellationToken = default)
    {
        var result = new TaskListResult
        {
            StartTime = DateTime.UtcNow
        };

        _logger.Info($"Starting task list execution: {config}");
        var stopwatch = Stopwatch.StartNew();

        var enabledTasks = config.Tasks.Where(t => t.Enabled).ToList();
        
        if (enabledTasks.Count == 0)
        {
            _logger.Warning("No enabled tasks to execute");
            result.Success = true;
            result.EndTime = DateTime.UtcNow;
            result.TotalDuration = stopwatch.Elapsed;
            return result;
        }

        if (config.MaxParallelTasks > 1)
        {
            await ExecuteParallelAsync(config, enabledTasks, result, cancellationToken);
        }
        else
        {
            await ExecuteSequentialAsync(config, enabledTasks, result, cancellationToken);
        }

        stopwatch.Stop();
        result.TotalDuration = stopwatch.Elapsed;
        result.EndTime = DateTime.UtcNow;
        result.Success = result.TasksFailed == 0;

        _logger.Info($"Task list completed: {result.TasksCompleted} succeeded, {result.TasksFailed} failed, {result.TasksSkipped} skipped");
        _logger.Info($"Total duration: {result.TotalDuration.TotalSeconds:F2}s, Total bytes: {FormatBytes(result.TotalBytesTransferred)}");

        return result;
    }

    private async Task ExecuteSequentialAsync(
        TaskListConfig config, 
        List<SyncTask> tasks, 
        TaskListResult result, 
        CancellationToken cancellationToken)
    {
        int taskIndex = 0;
        foreach (var task in tasks)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                _logger.Info("Task list execution cancelled");
                break;
            }

            taskIndex++;
            _logger.Info($"Executing task [{taskIndex}/{tasks.Count}]: {task.Name}");

            var taskResult = await ExecuteTaskAsync(task, cancellationToken);
            result.TaskResults.Add(taskResult);

            if (taskResult.Skipped)
            {
                result.TasksSkipped++;
            }
            else if (taskResult.Success)
            {
                result.TasksCompleted++;
                result.TotalBytesTransferred += taskResult.SyncResult?.BytesTransferred ?? 0;
            }
            else
            {
                result.TasksFailed++;
                result.Errors.Add($"Task '{task.Name}' failed: {taskResult.ErrorMessage}");

                if (config.StopOnError)
                {
                    _logger.Error($"Stopping due to error in task: {task.Name}");
                    break;
                }
            }

            OnTaskProgress(task.Name, taskIndex, tasks.Count, taskResult);
        }
    }

    private async Task ExecuteParallelAsync(
        TaskListConfig config, 
        List<SyncTask> tasks, 
        TaskListResult result, 
        CancellationToken cancellationToken)
    {
        using var semaphore = new SemaphoreSlim(config.MaxParallelTasks);
        var lockObj = new object();
        int taskIndex = 0;
        int completedIndex = 0;
        int shouldStop = 0;

        var taskExecutions = tasks.Select(async task =>
        {
            if (cancellationToken.IsCancellationRequested || Volatile.Read(ref shouldStop) == 1)
            {
                lock (lockObj)
                {
                    result.TasksSkipped++;
                    result.TaskResults.Add(new TaskResult
                    {
                        TaskName = task.Name,
                        Skipped = true
                    });
                }
                return;
            }

            await semaphore.WaitAsync(cancellationToken);
            try
            {
                int currentIndex;
                lock (lockObj)
                {
                    taskIndex++;
                    currentIndex = taskIndex;
                }

                _logger.Info($"Executing task [{currentIndex}/{tasks.Count}]: {task.Name}");

                var taskResult = await ExecuteTaskAsync(task, cancellationToken);

                lock (lockObj)
                {
                    completedIndex++;
                    result.TaskResults.Add(taskResult);

                    if (taskResult.Skipped)
                    {
                        result.TasksSkipped++;
                    }
                    else if (taskResult.Success)
                    {
                        result.TasksCompleted++;
                        result.TotalBytesTransferred += taskResult.SyncResult?.BytesTransferred ?? 0;
                    }
                    else
                    {
                        result.TasksFailed++;
                        result.Errors.Add($"Task '{task.Name}' failed: {taskResult.ErrorMessage}");

                        if (config.StopOnError)
                        {
                            Volatile.Write(ref shouldStop, 1);
                        }
                    }

                    OnTaskProgress(task.Name, completedIndex, tasks.Count, taskResult);
                }
            }
            finally
            {
                semaphore.Release();
            }
        }).ToList();

        await Task.WhenAll(taskExecutions);
    }

    public async Task<TaskResult> ExecuteTaskAsync(SyncTask task, CancellationToken cancellationToken = default)
    {
        var result = new TaskResult
        {
            TaskName = task.Name
        };

        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (string.IsNullOrEmpty(task.Options.SourcePath))
            {
                result.Skipped = true;
                result.ErrorMessage = "Source path is not specified";
                _logger.Warning($"Task '{task.Name}' skipped: source path not specified");
                return result;
            }

            if (string.IsNullOrEmpty(task.Options.TargetPath))
            {
                result.Skipped = true;
                result.ErrorMessage = "Target path is not specified";
                _logger.Warning($"Task '{task.Name}' skipped: target path not specified");
                return result;
            }

            if (!Directory.Exists(task.Options.SourcePath))
            {
                result.Skipped = true;
                result.ErrorMessage = $"Source path does not exist: {task.Options.SourcePath}";
                _logger.Warning($"Task '{task.Name}' skipped: {result.ErrorMessage}");
                return result;
            }

            using var fileFilter = new FileFilter(_logger, task.Options.SourcePath, task.Options.GitIgnore);
            
            foreach (var pattern in task.Options.IncludePatterns)
            {
                fileFilter.AddIncludePattern(pattern);
            }

            foreach (var pattern in task.Options.ExcludePatterns)
            {
                fileFilter.AddExcludePattern(pattern);
            }

            using var syncEngine = _syncEngineFactory(_logger, fileFilter);
            
            syncEngine.ProgressChanged += (sender, e) =>
            {
                OnFileProgress(task.Name, e.CurrentFile, e.ProcessedFiles, e.TotalFiles, e.ProcessedBytes, e.TotalBytes, e.Action);
            };
            
            var syncResult = await syncEngine.SyncAsync(task.Options, cancellationToken);
            
            result.Success = syncResult.Success;
            result.SyncResult = syncResult;
            
            if (!syncResult.Success && syncResult.Errors.Count > 0)
            {
                result.ErrorMessage = string.Join("; ", syncResult.Errors);
            }
        }
        catch (OperationCanceledException)
        {
            result.Success = false;
            result.ErrorMessage = "Task was cancelled";
            _logger.Info($"Task '{task.Name}' was cancelled");
        }
        catch (Exception ex)
        {
            result.Success = false;
            result.ErrorMessage = ex.Message;
            _logger.Error($"Task '{task.Name}' failed with exception", ex);
        }

        stopwatch.Stop();
        result.Duration = stopwatch.Elapsed;

        return result;
    }

    private void OnTaskProgress(string taskName, int current, int total, TaskResult taskResult)
    {
        TaskProgress?.Invoke(this, new TaskProgressEventArgs
        {
            TaskName = taskName,
            CurrentTask = current,
            TotalTasks = total,
            Result = taskResult
        });
    }
    
    private void OnFileProgress(string taskName, string currentFile, long processedFiles, long totalFiles, long processedBytes, long totalBytes, SyncAction action)
    {
        TaskProgress?.Invoke(this, new TaskProgressEventArgs
        {
            TaskName = taskName,
            CurrentFile = currentFile,
            ProcessedFiles = processedFiles,
            TotalFiles = totalFiles,
            ProcessedBytes = processedBytes,
            TotalBytes = totalBytes,
            Action = action
        });
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int i = 0;
        double size = bytes;
        while (size >= 1024 && i < suffixes.Length - 1)
        {
            size /= 1024;
            i++;
        }
        return $"{size:F2} {suffixes[i]}";
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
        }
    }
}

public class TaskProgressEventArgs : EventArgs
{
    public string TaskName { get; set; } = string.Empty;
    public int CurrentTask { get; set; }
    public int TotalTasks { get; set; }
    public TaskResult Result { get; set; } = new();
    public string? CurrentFile { get; set; }
    public long ProcessedFiles { get; set; }
    public long TotalFiles { get; set; }
    public long ProcessedBytes { get; set; }
    public long TotalBytes { get; set; }
    public SyncAction Action { get; set; }
}
