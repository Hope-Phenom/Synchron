using System.Text;
using System.Text.Json;
using Synchron.Core.Interfaces;
using Synchron.Core.Models;

namespace Synchron.Core;

public class TaskListManager
{
    private readonly ILogger _logger;

    public TaskListManager(ILogger logger)
    {
        _logger = logger;
    }

    public TaskListConfig Load(string configPath)
    {
        if (!File.Exists(configPath))
        {
            throw new FileNotFoundException($"Task list config file not found: {configPath}");
        }

        try
        {
            var json = File.ReadAllText(configPath, Encoding.UTF8);
            var config = JsonSerializer.Deserialize<TaskListConfig>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            });

            if (config == null)
            {
                throw new InvalidOperationException("Failed to parse task list config file");
            }

            _logger.Info($"Loaded task list config: {configPath}");
            _logger.Info($"Found {config.TotalTaskCount} tasks ({config.EnabledTaskCount} enabled)");

            return config;
        }
        catch (JsonException ex)
        {
            _logger.Error($"Failed to parse task list config: {configPath}", ex);
            throw new InvalidOperationException($"Invalid JSON format in task list config: {ex.Message}", ex);
        }
    }

    public void Save(TaskListConfig config, string configPath)
    {
        try
        {
            var directory = Path.GetDirectoryName(configPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(config, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
            });

            File.WriteAllText(configPath, json, Encoding.UTF8);
            _logger.Info($"Task list config saved to: {configPath}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to save task list config: {configPath}", ex);
            throw;
        }
    }

    public ValidationResult Validate(TaskListConfig config)
    {
        var result = new ValidationResult { IsValid = true };

        if (config.Tasks.Count == 0)
        {
            result.Warnings.Add("No tasks defined in the configuration");
        }

        var taskNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        int index = 0;

        foreach (var task in config.Tasks)
        {
            index++;

            if (string.IsNullOrWhiteSpace(task.Name))
            {
                result.Errors.Add($"Task #{index}: Name is required");
                result.IsValid = false;
            }
            else if (taskNames.Contains(task.Name))
            {
                result.Errors.Add($"Task '{task.Name}': Duplicate task name");
                result.IsValid = false;
            }
            else
            {
                taskNames.Add(task.Name);
            }

            if (task.Enabled)
            {
                if (string.IsNullOrWhiteSpace(task.Options.SourcePath))
                {
                    result.Warnings.Add($"Task '{task.Name}': Source path is not specified (will be skipped)");
                }
                else if (!Directory.Exists(task.Options.SourcePath))
                {
                    result.Warnings.Add($"Task '{task.Name}': Source path does not exist: {task.Options.SourcePath} (will be skipped)");
                }

                if (string.IsNullOrWhiteSpace(task.Options.TargetPath))
                {
                    result.Warnings.Add($"Task '{task.Name}': Target path is not specified (will be skipped)");
                }
            }

            if (task.Options.BufferSize < 1024)
            {
                result.Warnings.Add($"Task '{task.Name}': Buffer size should be at least 1024 bytes");
            }

            if (task.Options.MaxRetries < 0)
            {
                result.Errors.Add($"Task '{task.Name}': Max retries cannot be negative");
                result.IsValid = false;
            }
        }

        if (config.MaxParallelTasks < 1)
        {
            result.Warnings.Add("MaxParallelTasks should be at least 1, using default value 1");
            config.MaxParallelTasks = 1;
        }

        return result;
    }

    public TaskListConfig CreateSampleConfig()
    {
        return new TaskListConfig
        {
            Name = "Sample Task List",
            StopOnError = false,
            MaxParallelTasks = 1,
            Tasks = new List<SyncTask>
            {
                new SyncTask
                {
                    Name = "Documents Backup",
                    Description = "Sync documents to backup folder",
                    Enabled = true,
                    Options = new SyncOptions
                    {
                        SourcePath = "C:\\Users\\User\\Documents",
                        TargetPath = "D:\\Backup\\Documents",
                        Mode = SyncMode.Sync,
                        IncludeSubdirectories = true,
                        GitIgnore = new GitIgnoreOptions { Enabled = true, AutoDetect = true }
                    }
                },
                new SyncTask
                {
                    Name = "Photos Backup",
                    Description = "Mirror photos to external drive",
                    Enabled = true,
                    Options = new SyncOptions
                    {
                        SourcePath = "D:\\Photos",
                        TargetPath = "E:\\Photos",
                        Mode = SyncMode.Mirror,
                        IncludeSubdirectories = true
                    }
                },
                new SyncTask
                {
                    Name = "Project Sync",
                    Description = "Sync project files (disabled example)",
                    Enabled = false,
                    Options = new SyncOptions
                    {
                        SourcePath = "C:\\Projects",
                        TargetPath = "D:\\Backup\\Projects",
                        Mode = SyncMode.Sync
                    }
                }
            }
        };
    }

    public List<string> ListTasks(TaskListConfig config)
    {
        var list = new List<string>();
        
        list.Add($"Task List: {config.Name ?? "Unnamed"}");
        list.Add(new string('-', 50));
        list.Add($"Total: {config.TotalTaskCount}, Enabled: {config.EnabledTaskCount}");
        list.Add($"StopOnError: {config.StopOnError}, MaxParallel: {config.MaxParallelTasks}");
        list.Add("");

        int index = 0;
        foreach (var task in config.Tasks)
        {
            index++;
            var status = task.Enabled ? "[*]" : "[ ]";
            list.Add($"{index}. {status} {task.Name}");
            
            if (!string.IsNullOrEmpty(task.Description))
            {
                list.Add($"   Description: {task.Description}");
            }
            
            list.Add($"   Source: {task.Options.SourcePath}");
            list.Add($"   Target: {task.Options.TargetPath}");
            list.Add($"   Mode: {task.Options.Mode}");
            
            if (task.Options.GitIgnore.Enabled)
            {
                list.Add($"   GitIgnore: enabled");
            }
            
            list.Add("");
        }

        return list;
    }
}

public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();

    public override string ToString()
    {
        if (IsValid && Warnings.Count == 0)
        {
            return "Valid";
        }

        var parts = new List<string>();
        
        if (!IsValid)
        {
            parts.Add($"Invalid ({Errors.Count} errors)");
        }
        
        if (Warnings.Count > 0)
        {
            parts.Add($"{Warnings.Count} warnings");
        }

        return string.Join(", ", parts);
    }
}
