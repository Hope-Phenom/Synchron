using Xunit;
using Synchron.Core;
using Synchron.Core.Interfaces;
using Synchron.Core.Models;

namespace Synchron.Core.Tests;

public class TaskListManagerTests
{
    private readonly ILogger _logger;
    private readonly TaskListManager _manager;

    public TaskListManagerTests()
    {
        _logger = new Logger(LogLevel.None);
        _manager = new TaskListManager(_logger);
    }

    [Fact]
    public void CreateSampleConfig_ShouldReturnValidConfig()
    {
        var config = _manager.CreateSampleConfig();

        Assert.NotNull(config);
        Assert.Equal("Sample Task List", config.Name);
        Assert.True(config.Tasks.Count >= 2);
        Assert.Contains(config.Tasks, t => t.Name == "Documents Backup");
    }

    [Fact]
    public void Validate_WithValidConfig_ShouldReturnValid()
    {
        var config = new TaskListConfig
        {
            Tasks = new List<SyncTask>
            {
                new SyncTask
                {
                    Name = "Test Task",
                    Enabled = true,
                    Options = new SyncOptions
                    {
                        SourcePath = Directory.GetCurrentDirectory(),
                        TargetPath = Path.GetTempPath()
                    }
                }
            }
        };

        var result = _manager.Validate(config);

        Assert.True(result.IsValid);
    }

    [Fact]
    public void Validate_WithDuplicateNames_ShouldReturnInvalid()
    {
        var config = new TaskListConfig
        {
            Tasks = new List<SyncTask>
            {
                new SyncTask { Name = "Duplicate", Enabled = true },
                new SyncTask { Name = "Duplicate", Enabled = true }
            }
        };

        var result = _manager.Validate(config);

        Assert.False(result.IsValid);
        Assert.Contains(result.Errors, e => e.Contains("Duplicate"));
    }

    [Fact]
    public void Validate_WithEmptyName_ShouldReturnInvalid()
    {
        var config = new TaskListConfig
        {
            Tasks = new List<SyncTask>
            {
                new SyncTask { Name = "", Enabled = true }
            }
        };

        var result = _manager.Validate(config);

        Assert.False(result.IsValid);
    }

    [Fact]
    public void Validate_WithMissingSourcePath_ShouldAddWarning()
    {
        var config = new TaskListConfig
        {
            Tasks = new List<SyncTask>
            {
                new SyncTask
                {
                    Name = "Test",
                    Enabled = true,
                    Options = new SyncOptions
                    {
                        TargetPath = Path.GetTempPath()
                    }
                }
            }
        };

        var result = _manager.Validate(config);

        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Contains("Source path"));
    }

    [Fact]
    public void Validate_WithNonExistentSourcePath_ShouldAddWarning()
    {
        var config = new TaskListConfig
        {
            Tasks = new List<SyncTask>
            {
                new SyncTask
                {
                    Name = "Test",
                    Enabled = true,
                    Options = new SyncOptions
                    {
                        SourcePath = "C:\\NonExistent\\Path\\12345",
                        TargetPath = Path.GetTempPath()
                    }
                }
            }
        };

        var result = _manager.Validate(config);

        Assert.True(result.IsValid);
        Assert.Contains(result.Warnings, w => w.Contains("does not exist"));
    }

    [Fact]
    public void Validate_WithDisabledTask_ShouldNotWarnAboutMissingPath()
    {
        var config = new TaskListConfig
        {
            Tasks = new List<SyncTask>
            {
                new SyncTask
                {
                    Name = "Test",
                    Enabled = false,
                    Options = new SyncOptions()
                }
            }
        };

        var result = _manager.Validate(config);

        Assert.DoesNotContain(result.Warnings, w => w.Contains("Source path"));
    }

    [Fact]
    public void ListTasks_ShouldReturnFormattedList()
    {
        var config = new TaskListConfig
        {
            Name = "Test List",
            Tasks = new List<SyncTask>
            {
                new SyncTask
                {
                    Name = "Task 1",
                    Description = "First task",
                    Enabled = true,
                    Options = new SyncOptions
                    {
                        SourcePath = "C:\\Source",
                        TargetPath = "D:\\Target",
                        Mode = SyncMode.Sync
                    }
                },
                new SyncTask
                {
                    Name = "Task 2",
                    Enabled = false,
                    Options = new SyncOptions
                    {
                        SourcePath = "C:\\Source2",
                        TargetPath = "D:\\Target2"
                    }
                }
            }
        };

        var list = _manager.ListTasks(config);

        Assert.Contains("Task List: Test List", list);
        Assert.True(list.Any(l => l.Contains("Task 1")));
        Assert.True(list.Any(l => l.Contains("Task 2")));
        Assert.True(list.Any(l => l.Contains("First task")));
        Assert.True(list.Any(l => l.Contains("[*]")));
        Assert.True(list.Any(l => l.Contains("[ ]")));
    }

    [Fact]
    public void SaveAndLoad_ShouldPreserveConfig()
    {
        var config = new TaskListConfig
        {
            Name = "Test Config",
            StopOnError = false,
            Tasks = new List<SyncTask>
            {
                new SyncTask
                {
                    Name = "Backup",
                    Description = "Backup task",
                    Enabled = true,
                    Options = new SyncOptions
                    {
                        SourcePath = "C:\\Source",
                        TargetPath = "D:\\Target",
                        Mode = SyncMode.Mirror
                    }
                }
            }
        };

        var tempFile = Path.GetTempFileName();
        try
        {
            _manager.Save(config, tempFile);
            var loaded = _manager.Load(tempFile);

            Assert.Equal(config.Name, loaded.Name);
            Assert.Equal(config.StopOnError, loaded.StopOnError);
            Assert.Single(loaded.Tasks);
            Assert.Equal("Backup", loaded.Tasks[0].Name);
            Assert.Equal(SyncMode.Mirror, loaded.Tasks[0].Options.Mode);
        }
        finally
        {
            File.Delete(tempFile);
        }
    }

    [Fact]
    public void Load_WithNonExistentFile_ShouldThrow()
    {
        Assert.Throws<FileNotFoundException>(() => _manager.Load("nonexistent.json"));
    }

    [Fact]
    public void Load_WithInvalidJson_ShouldThrow()
    {
        var tempFile = Path.GetTempFileName();
        try
        {
            File.WriteAllText(tempFile, "{ invalid json }");
            Assert.Throws<InvalidOperationException>(() => _manager.Load(tempFile));
        }
        finally
        {
            File.Delete(tempFile);
        }
    }
}
