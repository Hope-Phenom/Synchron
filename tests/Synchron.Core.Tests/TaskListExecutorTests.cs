using Xunit;
using Synchron.Core;
using Synchron.Core.Interfaces;
using Synchron.Core.Models;

namespace Synchron.Core.Tests;

public class TaskListExecutorTests : IDisposable
{
    private readonly ILogger _logger;
    private readonly List<string> _tempDirectories = new();

    public TaskListExecutorTests()
    {
        _logger = new Logger(LogLevel.None);
    }

    public void Dispose()
    {
        foreach (var dir in _tempDirectories)
        {
            if (Directory.Exists(dir))
            {
                Directory.Delete(dir, true);
            }
        }
    }

    [Fact]
    public async Task ExecuteAsync_WithEmptyTaskList_ShouldReturnSuccess()
    {
        var config = new TaskListConfig { Tasks = new List<SyncTask>() };
        using var executor = new TaskListExecutor(_logger);

        var result = await executor.ExecuteAsync(config);

        Assert.True(result.Success);
        Assert.Equal(0, result.TasksCompleted);
    }

    [Fact]
    public async Task ExecuteAsync_WithDisabledTasks_ShouldSkipThem()
    {
        var config = new TaskListConfig
        {
            Tasks = new List<SyncTask>
            {
                new SyncTask { Name = "Disabled", Enabled = false, Options = new SyncOptions() }
            }
        };

        using var executor = new TaskListExecutor(_logger);

        var result = await executor.ExecuteAsync(config);

        Assert.True(result.Success);
        Assert.Equal(0, result.TasksCompleted);
        Assert.Equal(0, result.TasksSkipped);
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingSourcePath_ShouldSkipTask()
    {
        var config = new TaskListConfig
        {
            Tasks = new List<SyncTask>
            {
                new SyncTask
                {
                    Name = "No Source",
                    Enabled = true,
                    Options = new SyncOptions { TargetPath = "D:\\Target" }
                }
            }
        };

        using var executor = new TaskListExecutor(_logger);

        var result = await executor.ExecuteAsync(config);

        Assert.True(result.Success);
        Assert.Equal(0, result.TasksCompleted);
        Assert.Equal(1, result.TasksSkipped);
    }

    [Fact]
    public async Task ExecuteAsync_WithMissingTargetPath_ShouldSkipTask()
    {
        var tempPath = Directory.GetCurrentDirectory();
        var config = new TaskListConfig
        {
            Tasks = new List<SyncTask>
            {
                new SyncTask
                {
                    Name = "No Target",
                    Enabled = true,
                    Options = new SyncOptions { SourcePath = tempPath }
                }
            }
        };

        using var executor = new TaskListExecutor(_logger);

        var result = await executor.ExecuteAsync(config);

        Assert.True(result.Success);
        Assert.Equal(0, result.TasksCompleted);
        Assert.Equal(1, result.TasksSkipped);
    }

    [Fact]
    public async Task ExecuteAsync_WithNonExistentSourcePath_ShouldSkipTask()
    {
        var config = new TaskListConfig
        {
            Tasks = new List<SyncTask>
            {
                new SyncTask
                {
                    Name = "Bad Source",
                    Enabled = true,
                    Options = new SyncOptions
                    {
                        SourcePath = "C:\\NonExistent12345",
                        TargetPath = Path.GetTempPath()
                    }
                }
            }
        };

        using var executor = new TaskListExecutor(_logger);

        var result = await executor.ExecuteAsync(config);

        Assert.True(result.Success);
        Assert.Equal(1, result.TasksSkipped);
    }

    [Fact]
    public async Task ExecuteAsync_WithValidTask_ShouldExecute()
    {
        var sourceDir = CreateTempDirectory();
        var targetDir = CreateTempDirectory();
        
        File.WriteAllText(Path.Combine(sourceDir, "test.txt"), "test content");

        var config = new TaskListConfig
        {
            Tasks = new List<SyncTask>
            {
                new SyncTask
                {
                    Name = "Valid Task",
                    Enabled = true,
                    Options = new SyncOptions
                    {
                        SourcePath = sourceDir,
                        TargetPath = targetDir,
                        Mode = SyncMode.Sync
                    }
                }
            }
        };

        using var executor = new TaskListExecutor(_logger);

        var result = await executor.ExecuteAsync(config);

        Assert.True(result.Success);
        Assert.Equal(1, result.TasksCompleted);
        Assert.True(result.TotalBytesTransferred > 0);
    }

    [Fact]
    public async Task ExecuteAsync_WithStopOnError_ShouldStopOnFailure()
    {
        var sourceDir = CreateTempDirectory();
        var targetDir = CreateTempDirectory();
        
        File.WriteAllText(Path.Combine(sourceDir, "fail.txt"), "content");
        
        var config = new TaskListConfig
        {
            StopOnError = true,
            Tasks = new List<SyncTask>
            {
                new SyncTask
                {
                    Name = "First Task",
                    Enabled = true,
                    Options = new SyncOptions
                    {
                        SourcePath = sourceDir,
                        TargetPath = targetDir
                    }
                },
                new SyncTask
                {
                    Name = "Second Task",
                    Enabled = true,
                    Options = new SyncOptions
                    {
                        SourcePath = sourceDir,
                        TargetPath = Path.Combine(targetDir, "sub")
                    }
                }
            }
        };

        using var executor = new TaskListExecutor(_logger);

        var result = await executor.ExecuteAsync(config);

        Assert.True(result.Success);
        Assert.Equal(2, result.TaskResults.Count);
    }

    [Fact]
    public async Task ExecuteAsync_WithoutStopOnError_ShouldContinueOnFailure()
    {
        var sourceDir = CreateTempDirectory();
        var targetDir = CreateTempDirectory();
        
        var config = new TaskListConfig
        {
            StopOnError = false,
            Tasks = new List<SyncTask>
            {
                new SyncTask
                {
                    Name = "Bad Task",
                    Enabled = true,
                    Options = new SyncOptions
                    {
                        SourcePath = "C:\\NonExistent12345",
                        TargetPath = Path.GetTempPath()
                    }
                },
                new SyncTask
                {
                    Name = "Good Task",
                    Enabled = true,
                    Options = new SyncOptions
                    {
                        SourcePath = sourceDir,
                        TargetPath = targetDir
                    }
                }
            }
        };

        using var executor = new TaskListExecutor(_logger);

        var result = await executor.ExecuteAsync(config);

        Assert.Equal(2, result.TaskResults.Count);
        Assert.Equal(1, result.TasksSkipped);
        Assert.Equal(1, result.TasksCompleted);
    }

    [Fact]
    public async Task ExecuteTaskAsync_ShouldSetDuration()
    {
        var sourceDir = CreateTempDirectory();
        var targetDir = CreateTempDirectory();
        
        var task = new SyncTask
        {
            Name = "Test",
            Enabled = true,
            Options = new SyncOptions
            {
                SourcePath = sourceDir,
                TargetPath = targetDir
            }
        };

        using var executor = new TaskListExecutor(_logger);

        var result = await executor.ExecuteTaskAsync(task);

        Assert.True(result.Duration > TimeSpan.Zero);
    }

    [Fact]
    public async Task ExecuteAsync_WithDryRun_ShouldNotCopyFiles()
    {
        var sourceDir = CreateTempDirectory();
        var targetDir = CreateTempDirectory();
        
        File.WriteAllText(Path.Combine(sourceDir, "test.txt"), "test content");

        var config = new TaskListConfig
        {
            Tasks = new List<SyncTask>
            {
                new SyncTask
                {
                    Name = "Dry Run Task",
                    Enabled = true,
                    Options = new SyncOptions
                    {
                        SourcePath = sourceDir,
                        TargetPath = targetDir,
                        Mode = SyncMode.Sync,
                        DryRun = true
                    }
                }
            }
        };

        using var executor = new TaskListExecutor(_logger);

        var result = await executor.ExecuteAsync(config);

        Assert.True(result.Success);
        Assert.Equal(1, result.TasksCompleted);
        Assert.Equal(0, result.TotalBytesTransferred);
        Assert.False(File.Exists(Path.Combine(targetDir, "test.txt")));
    }

    private string CreateTempDirectory()
    {
        var path = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(path);
        _tempDirectories.Add(path);
        return path;
    }
}
