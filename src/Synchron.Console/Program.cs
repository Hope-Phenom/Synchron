using System.Text.Json;
using Synchron.Core;
using Synchron.Core.Interfaces;
using Synchron.Core.Models;

namespace Synchron.Console;

public static class Program
{
    private static ILogger? _logger;
    private static SyncEngine? _syncEngine;
    private static ConfigManager? _configManager;
    private static FileFilter? _fileFilter;
    private static CancellationTokenSource? _cancellationTokenSource;

    public static async Task<int> Main(string[] args)
    {
        var cmdOptions = CommandLineParser.Parse(args);

        if (cmdOptions.ShowHelp)
        {
            if (cmdOptions.IsTaskListMode)
            {
                CommandLineParser.ShowTaskListHelp();
            }
            else
            {
                CommandLineParser.ShowHelp();
            }
            return 0;
        }

        if (cmdOptions.ShowVersion)
        {
            CommandLineParser.ShowVersion();
            return 0;
        }

        if (cmdOptions.IsTaskListMode)
        {
            return await RunTaskListMode(cmdOptions);
        }

        InitializeServices(cmdOptions);

        var syncOptions = BuildSyncOptions(cmdOptions);

        if (!string.IsNullOrEmpty(cmdOptions.ConfigPath))
        {
            var configOptions = _configManager!.Load(cmdOptions.ConfigPath);
            syncOptions = MergeOptions(configOptions, syncOptions, cmdOptions);
        }

        ApplyGitIgnoreOptions(syncOptions, cmdOptions);

        if (string.IsNullOrEmpty(syncOptions.SourcePath) || string.IsNullOrEmpty(syncOptions.TargetPath))
        {
            if (!cmdOptions.ShowHelp)
            {
                System.Console.WriteLine("Starting interactive mode...");
                System.Console.WriteLine();
            }
            return await RunInteractiveMode(syncOptions);
        }

        InitializeFileFilterWithGitIgnore(syncOptions);

        if (cmdOptions.Watch)
        {
            return await RunWatchMode(syncOptions);
        }

        return await RunSyncMode(syncOptions, cmdOptions.DryRun);
    }

    private static async Task<int> RunTaskListMode(CommandLineOptions cmdOptions)
    {
        using var logger = new Logger(
            cmdOptions.LogLevel ?? (cmdOptions.Verbose ? LogLevel.Debug : LogLevel.Info),
            cmdOptions.LogFilePath);

        if (cmdOptions.InitTaskList)
        {
            return CreateSampleTaskList(logger);
        }

        if (string.IsNullOrEmpty(cmdOptions.TaskListPath))
        {
            System.Console.WriteLine("Error: Task list config file path is required.");
            System.Console.WriteLine("Usage: Synchron task <tasks.json> [options]");
            return 1;
        }

        var taskListPath = Path.GetFullPath(cmdOptions.TaskListPath);
        var taskListManager = new TaskListManager(logger);

        TaskListConfig config;
        try
        {
            config = taskListManager.Load(taskListPath);
        }
        catch (FileNotFoundException ex)
        {
            logger.Error(ex.Message);
            return 1;
        }
        catch (InvalidOperationException ex)
        {
            logger.Error($"Failed to parse task list config: {ex.Message}");
            return 1;
        }

        if (cmdOptions.ListTasks)
        {
            var taskList = taskListManager.ListTasks(config);
            foreach (var line in taskList)
            {
                System.Console.WriteLine(line);
            }
            return 0;
        }

        var validation = taskListManager.Validate(config);
        if (!validation.IsValid)
        {
            logger.Error("Task list validation failed:");
            foreach (var error in validation.Errors)
            {
                logger.Error($"  - {error}");
            }
            return 1;
        }

        if (validation.Warnings.Count > 0)
        {
            logger.Warning("Task list validation warnings:");
            foreach (var warning in validation.Warnings)
            {
                logger.Warning($"  - {warning}");
            }
        }

        var executionConfig = CloneConfig(config);

        if (!string.IsNullOrEmpty(cmdOptions.TaskName))
        {
            var task = executionConfig.Tasks.FirstOrDefault(t => 
                t.Name.Equals(cmdOptions.TaskName, StringComparison.OrdinalIgnoreCase));
            
            if (task == null)
            {
                logger.Error($"Task not found: {cmdOptions.TaskName}");
                return 1;
            }

            if (!task.Enabled)
            {
                logger.Warning($"Task '{task.Name}' is disabled. Use --list to see all tasks.");
                return 0;
            }

            executionConfig.Tasks = new List<SyncTask> { task };
            executionConfig.StopOnError = true;
        }

        if (cmdOptions.DryRun)
        {
            foreach (var task in executionConfig.Tasks.Where(t => t.Enabled))
            {
                task.Options.DryRun = true;
            }
        }

        _cancellationTokenSource = new CancellationTokenSource();
        System.Console.CancelKeyPress += (sender, e) =>
        {
            e.Cancel = true;
            logger.Info("Cancellation requested, stopping tasks...");
            _cancellationTokenSource.Cancel();
        };

        using var executor = new TaskListExecutor(logger);
        var result = await executor.ExecuteAsync(executionConfig, _cancellationTokenSource.Token);

        PrintTaskListResult(result);

        return result.Success ? 0 : 1;
    }

    private static TaskListConfig CloneConfig(TaskListConfig config)
    {
        var json = JsonSerializer.Serialize(config);
        return JsonSerializer.Deserialize<TaskListConfig>(json) ?? new TaskListConfig();
    }

    private static int CreateSampleTaskList(ILogger logger)
    {
        var taskListManager = new TaskListManager(logger);
        var sampleConfig = taskListManager.CreateSampleConfig();
        
        var outputPath = Path.Combine(Directory.GetCurrentDirectory(), "tasks.json");
        
        if (File.Exists(outputPath))
        {
            System.Console.WriteLine($"File already exists: {outputPath}");
            System.Console.Write("Overwrite? (y/N): ");
            var input = System.Console.ReadLine();
            if (!input?.Equals("y", StringComparison.OrdinalIgnoreCase) == true)
            {
                System.Console.WriteLine("Cancelled.");
                return 0;
            }
        }

        taskListManager.Save(sampleConfig, outputPath);
        
        System.Console.WriteLine($"Sample task list created: {outputPath}");
        System.Console.WriteLine();
        System.Console.WriteLine("Edit the file to configure your sync tasks, then run:");
        System.Console.WriteLine($"  Synchron task {outputPath}");
        
        return 0;
    }

    private static void PrintTaskListResult(TaskListResult result)
    {
        System.Console.WriteLine();
        System.Console.WriteLine("═══════════════════════════════════════════════════════════");
        System.Console.WriteLine("                    Task List Execution                    ");
        System.Console.WriteLine("═══════════════════════════════════════════════════════════");

        int index = 0;
        foreach (var taskResult in result.TaskResults)
        {
            index++;
            var status = taskResult.Skipped ? "⊘" : (taskResult.Success ? "✓" : "✗");
            var statusText = taskResult.Skipped ? "Skipped" : (taskResult.Success ? "Success" : "Failed");
            
            System.Console.WriteLine();
            System.Console.WriteLine($"[{index}] {taskResult.TaskName}... {status}");
            
            if (taskResult.Skipped)
            {
                System.Console.WriteLine($"      Skipped: {taskResult.ErrorMessage}");
            }
            else if (taskResult.Success && taskResult.SyncResult != null)
            {
                var sync = taskResult.SyncResult;
                System.Console.WriteLine($"      ✓ {statusText} - {sync.FilesCopied} copied, {sync.FilesMoved} moved, {sync.FilesDeleted} deleted");
                System.Console.WriteLine($"      {FormatBytes(sync.BytesTransferred)}, {sync.Duration.TotalSeconds:F2}s");
            }
            else
            {
                System.Console.WriteLine($"      ✗ {statusText}: {taskResult.ErrorMessage}");
            }
        }

        System.Console.WriteLine();
        System.Console.WriteLine("═══════════════════════════════════════════════════════════");
        System.Console.WriteLine("                         Summary                           ");
        System.Console.WriteLine("═══════════════════════════════════════════════════════════");
        System.Console.WriteLine($"  Total Tasks:     {result.TaskResults.Count}");
        System.Console.WriteLine($"  Completed:       {result.TasksCompleted}");
        System.Console.WriteLine($"  Failed:          {result.TasksFailed}");
        System.Console.WriteLine($"  Skipped:         {result.TasksSkipped}");
        System.Console.WriteLine($"  Total Duration:  {result.TotalDuration.TotalSeconds:F2}s");
        System.Console.WriteLine($"  Total Data:      {FormatBytes(result.TotalBytesTransferred)}");
        System.Console.WriteLine("═══════════════════════════════════════════════════════════");

        if (result.Errors.Count > 0)
        {
            System.Console.WriteLine();
            System.Console.WriteLine("Errors:");
            foreach (var error in result.Errors)
            {
                System.Console.WriteLine($"  - {error}");
            }
        }
    }

    private static void InitializeServices(CommandLineOptions cmdOptions)
    {
        var logLevel = cmdOptions.LogLevel ?? (cmdOptions.Verbose ? LogLevel.Debug : LogLevel.Info);
        _logger = new Logger(logLevel, cmdOptions.LogFilePath);
        _configManager = new ConfigManager(_logger);
        _fileFilter = new FileFilter(_logger);
        _syncEngine = new SyncEngine(_logger, _fileFilter, _configManager);
    }

    private static SyncOptions BuildSyncOptions(CommandLineOptions cmdOptions)
    {
        var options = new SyncOptions
        {
            SourcePath = cmdOptions.SourcePath ?? string.Empty,
            TargetPath = cmdOptions.TargetPath ?? string.Empty,
            Mode = cmdOptions.Mode ?? SyncMode.Diff,
            WatchMode = cmdOptions.Watch,
            DryRun = cmdOptions.DryRun,
            VerifyHash = cmdOptions.VerifyHash,
            LogLevel = cmdOptions.LogLevel ?? LogLevel.Info,
            LogFilePath = cmdOptions.LogFilePath,
            IncludeSubdirectories = !cmdOptions.NoSubdirectories
        };

        if (cmdOptions.ConflictResolution.HasValue)
        {
            options.ConflictResolution = cmdOptions.ConflictResolution.Value;
        }

        if (cmdOptions.BufferSize > 0)
        {
            options.BufferSize = cmdOptions.BufferSize;
        }

        foreach (var pattern in cmdOptions.IncludePatterns)
        {
            options.IncludePatterns.Add(pattern);
            _fileFilter!.AddIncludePattern(pattern);
        }

        foreach (var pattern in cmdOptions.ExcludePatterns)
        {
            options.ExcludePatterns.Add(pattern);
            _fileFilter!.AddExcludePattern(pattern);
        }

        return options;
    }

    private static void ApplyGitIgnoreOptions(SyncOptions options, CommandLineOptions cmdOptions)
    {
        if (cmdOptions.NoGitIgnore)
        {
            options.GitIgnore.Enabled = false;
            _logger!.Info("GitIgnore disabled by command line");
            return;
        }

        if (!string.IsNullOrEmpty(cmdOptions.GitIgnoreFile))
        {
            options.GitIgnore.ExternalGitIgnorePath = cmdOptions.GitIgnoreFile;
            options.GitIgnore.OverrideAutoDetect = cmdOptions.ForceGitIgnore;
            _logger!.Info($"External GitIgnore specified: {cmdOptions.GitIgnoreFile}" + 
                         (cmdOptions.ForceGitIgnore ? " (with override)" : ""));
        }
    }

    private static void InitializeFileFilterWithGitIgnore(SyncOptions options)
    {
        if (!options.GitIgnore.Enabled || string.IsNullOrEmpty(options.SourcePath))
        {
            return;
        }

        _fileFilter!.Dispose();
        _fileFilter = new FileFilter(_logger!, options.SourcePath, options.GitIgnore);
        _syncEngine = new SyncEngine(_logger!, _fileFilter, _configManager!);

        foreach (var pattern in options.IncludePatterns)
        {
            _fileFilter.AddIncludePattern(pattern);
        }

        foreach (var pattern in options.ExcludePatterns)
        {
            _fileFilter.AddExcludePattern(pattern);
        }

        _logger!.Info($"GitIgnore filter initialized: {options.GitIgnore}");
    }

    private static SyncOptions MergeOptions(SyncOptions baseOptions, SyncOptions overrideOptions, CommandLineOptions cmdOptions)
    {
        if (!string.IsNullOrEmpty(overrideOptions.SourcePath))
            baseOptions.SourcePath = overrideOptions.SourcePath;

        if (!string.IsNullOrEmpty(overrideOptions.TargetPath))
            baseOptions.TargetPath = overrideOptions.TargetPath;

        if (overrideOptions.Mode != SyncMode.Diff)
            baseOptions.Mode = overrideOptions.Mode;

        if (overrideOptions.IncludePatterns.Count > 0)
            baseOptions.IncludePatterns = overrideOptions.IncludePatterns;

        if (overrideOptions.ExcludePatterns.Count > 0)
            baseOptions.ExcludePatterns = overrideOptions.ExcludePatterns;

        if (overrideOptions.WatchMode)
            baseOptions.WatchMode = true;

        if (overrideOptions.DryRun)
            baseOptions.DryRun = true;

        if (overrideOptions.VerifyHash)
            baseOptions.VerifyHash = true;

        if (!cmdOptions.NoGitIgnore && baseOptions.GitIgnore.Enabled)
        {
            if (overrideOptions.GitIgnore.Enabled)
            {
                baseOptions.GitIgnore = overrideOptions.GitIgnore.Clone();
            }
        }

        return baseOptions;
    }

    private static async Task<int> RunInteractiveMode(SyncOptions options)
    {
        using var menu = new InteractiveMenu(_logger!, _syncEngine!, _configManager!, _fileFilter!, options);
        await menu.RunAsync();
        return 0;
    }

    private static async Task<int> RunWatchMode(SyncOptions options)
    {
        _logger!.Info($"Starting watch mode: {options.SourcePath} -> {options.TargetPath}");

        using var watcher = new FileWatcher(_logger, _fileFilter!, _syncEngine!, options);
        watcher.FileChanged += (sender, e) =>
        {
            _logger.Info($"File changed: {e.FilePath} ({e.ChangeType})");
        };

        watcher.Start();

        System.Console.WriteLine("Watch mode active. Press 'q' to quit.");
        while (System.Console.ReadKey(true).Key != ConsoleKey.Q)
        {
            await Task.Delay(100);
        }

        watcher.Stop();
        return 0;
    }

    private static async Task<int> RunSyncMode(SyncOptions options, bool dryRun)
    {
        _logger!.Info($"Starting sync: {options.SourcePath} -> {options.TargetPath}");
        _logger.Info($"Mode: {options.Mode}, DryRun: {dryRun}");
        _logger.Info($"GitIgnore: {options.GitIgnore}");

        if (dryRun)
        {
            options.DryRun = true;
        }

        var result = await _syncEngine!.SyncAsync(options);

        System.Console.WriteLine();
        System.Console.WriteLine("═══════════════════════════════════════════");
        System.Console.WriteLine("              Sync Completed               ");
        System.Console.WriteLine("═══════════════════════════════════════════");
        System.Console.WriteLine($"  Status:     {(result.Success ? "Success" : "Failed")}");
        System.Console.WriteLine($"  Copied:     {result.FilesCopied}");
        System.Console.WriteLine($"  Moved:      {result.FilesMoved}");
        System.Console.WriteLine($"  Deleted:    {result.FilesDeleted}");
        System.Console.WriteLine($"  Skipped:    {result.FilesSkipped}");
        System.Console.WriteLine($"  Failed:     {result.FilesFailed}");
        System.Console.WriteLine($"  Transferred:{FormatBytes(result.BytesTransferred)}");
        System.Console.WriteLine($"  Duration:   {result.Duration.TotalSeconds:F2}s");
        System.Console.WriteLine($"  Speed:      {result.SpeedMBps:F2} MB/s");
        System.Console.WriteLine("═══════════════════════════════════════════");

        return result.Success ? 0 : 1;
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
}
