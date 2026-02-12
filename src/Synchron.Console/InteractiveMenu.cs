using Synchron.Core;
using Synchron.Core.Interfaces;
using Synchron.Core.Models;

namespace Synchron.Console;

public sealed class InteractiveMenu : IDisposable
{
    private readonly ILogger _logger;
    private readonly SyncEngine _syncEngine;
    private readonly ConfigManager _configManager;
    private readonly IFileFilter _fileFilter;
    private SyncOptions _options;
    private FileWatcher? _fileWatcher;
    private bool _disposed;

    public InteractiveMenu(ILogger logger, SyncEngine syncEngine, ConfigManager configManager, IFileFilter fileFilter, SyncOptions options)
    {
        _logger = logger;
        _syncEngine = syncEngine;
        _configManager = configManager;
        _fileFilter = fileFilter;
        _options = options;
    }

    public async Task RunAsync()
    {
        while (true)
        {
            ShowMenu();
            var input = System.Console.ReadLine()?.Trim();

            switch (input)
            {
                case "1":
                    await ExecuteSyncAsync();
                    break;
                case "2":
                    await PreviewSyncAsync();
                    break;
                case "3":
                    ConfigureSettings();
                    break;
                case "4":
                    StartWatchMode();
                    break;
                case "5":
                    StopWatchMode();
                    break;
                case "6":
                    ShowCurrentConfig();
                    break;
                case "7":
                    SaveCurrentConfig();
                    break;
                case "0":
                case "q":
                case "quit":
                case "exit":
                    StopWatchMode();
                    return;
                default:
                    System.Console.WriteLine("Invalid option. Please try again.");
                    break;
            }
        }
    }

    private void ShowMenu()
    {
        System.Console.WriteLine();
        System.Console.WriteLine("═══════════════════════════════════════════");
        System.Console.WriteLine("         Synchron - File Sync Tool         ");
        System.Console.WriteLine("═══════════════════════════════════════════");
        System.Console.WriteLine();
        System.Console.WriteLine($"  Source: {_options.SourcePath ?? "(not set)"}");
        System.Console.WriteLine($"  Target: {_options.TargetPath ?? "(not set)"}");
        System.Console.WriteLine($"  Mode:   {_options.Mode}");
        System.Console.WriteLine();
        System.Console.WriteLine("  [1] Execute Sync");
        System.Console.WriteLine("  [2] Preview Sync (Dry Run)");
        System.Console.WriteLine("  [3] Configure Settings");
        System.Console.WriteLine("  [4] Start Watch Mode");
        System.Console.WriteLine("  [5] Stop Watch Mode");
        System.Console.WriteLine("  [6] Show Current Configuration");
        System.Console.WriteLine("  [7] Save Configuration to File");
        System.Console.WriteLine("  [0] Exit");
        System.Console.WriteLine();
        System.Console.Write("  Select option: ");
    }

    private async Task ExecuteSyncAsync()
    {
        if (!ValidatePaths()) return;

        _logger.Info("Starting sync operation...");
        var result = await _syncEngine.SyncAsync(_options);
        DisplaySyncResult(result);
    }

    private async Task PreviewSyncAsync()
    {
        if (!ValidatePaths()) return;

        _logger.Info("Previewing sync operation...");
        var originalDryRun = _options.DryRun;
        _options.DryRun = true;
        var result = await _syncEngine.SyncAsync(_options);
        _options.DryRun = originalDryRun;
        DisplaySyncResult(result);
    }

    private void ConfigureSettings()
    {
        System.Console.WriteLine();
        System.Console.WriteLine("─── Configuration ───");
        System.Console.WriteLine();

        System.Console.Write($"Source path [{_options.SourcePath}]: ");
        var source = System.Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(source))
        {
            _options.SourcePath = source;
        }

        System.Console.Write($"Target path [{_options.TargetPath}]: ");
        var target = System.Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(target))
        {
            _options.TargetPath = target;
        }

        System.Console.WriteLine($"Sync modes: 1=Diff, 2=Sync, 3=Move, 4=Mirror");
        System.Console.Write($"Sync mode [{(int)_options.Mode + 1}]: ");
        var modeInput = System.Console.ReadLine()?.Trim();
        if (int.TryParse(modeInput, out var modeNum) && modeNum >= 1 && modeNum <= 4)
        {
            _options.Mode = (SyncMode)(modeNum - 1);
        }

        System.Console.Write($"Include subdirectories [{_options.IncludeSubdirectories}] (y/n): ");
        var subdirsInput = System.Console.ReadLine()?.Trim().ToLower();
        if (subdirsInput == "y") _options.IncludeSubdirectories = true;
        else if (subdirsInput == "n") _options.IncludeSubdirectories = false;

        System.Console.Write($"Include patterns (comma-separated) [{string.Join(",", _options.IncludePatterns)}]: ");
        var includeInput = System.Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(includeInput))
        {
            _options.IncludePatterns = includeInput.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToList();
            foreach (var pattern in _options.IncludePatterns)
            {
                _fileFilter.AddIncludePattern(pattern);
            }
        }

        System.Console.Write($"Exclude patterns (comma-separated) [{string.Join(",", _options.ExcludePatterns)}]: ");
        var excludeInput = System.Console.ReadLine()?.Trim();
        if (!string.IsNullOrEmpty(excludeInput))
        {
            _options.ExcludePatterns = excludeInput.Split(',').Select(p => p.Trim()).Where(p => !string.IsNullOrEmpty(p)).ToList();
            foreach (var pattern in _options.ExcludePatterns)
            {
                _fileFilter.AddExcludePattern(pattern);
            }
        }

        System.Console.WriteLine();
        _logger.Info("Configuration updated.");
    }

    private void StartWatchMode()
    {
        if (!ValidatePaths()) return;

        if (_fileWatcher != null && _fileWatcher.IsRunning)
        {
            _logger.Warning("Watch mode is already running.");
            return;
        }

        _fileWatcher = new FileWatcher(_logger, _fileFilter, _syncEngine, _options);
        _fileWatcher.FileChanged += OnFileChanged;
        _fileWatcher.Start();
        _logger.Info("Watch mode started. Press any key to stop...");
    }

    private void StopWatchMode()
    {
        if (_fileWatcher != null && _fileWatcher.IsRunning)
        {
            _fileWatcher.Stop();
            _fileWatcher.Dispose();
            _fileWatcher = null;
            _logger.Info("Watch mode stopped.");
        }
    }

    private void OnFileChanged(object? sender, FileChangedEventArgs e)
    {
        _logger.Info($"File changed: {e.FilePath} ({e.ChangeType})");
    }

    private void ShowCurrentConfig()
    {
        System.Console.WriteLine();
        System.Console.WriteLine("─── Current Configuration ───");
        System.Console.WriteLine($"  Source Path:        {_options.SourcePath}");
        System.Console.WriteLine($"  Target Path:        {_options.TargetPath}");
        System.Console.WriteLine($"  Sync Mode:          {_options.Mode}");
        System.Console.WriteLine($"  Include Subdirs:    {_options.IncludeSubdirectories}");
        System.Console.WriteLine($"  Compare Method:     {_options.CompareMethod}");
        System.Console.WriteLine($"  Conflict Resolution:{_options.ConflictResolution}");
        System.Console.WriteLine($"  Include Patterns:   {string.Join(", ", _options.IncludePatterns)}");
        System.Console.WriteLine($"  Exclude Patterns:   {string.Join(", ", _options.ExcludePatterns)}");
        System.Console.WriteLine($"  Buffer Size:        {_options.BufferSize} bytes");
        System.Console.WriteLine($"  Max Retries:        {_options.MaxRetries}");
        System.Console.WriteLine($"  Log Level:          {_options.LogLevel}");
        System.Console.WriteLine($"  Verify Hash:        {_options.VerifyHash}");
        System.Console.WriteLine($"  Preserve Timestamps:{_options.PreserveTimestamps}");
        System.Console.WriteLine();
    }

    private void SaveCurrentConfig()
    {
        System.Console.Write("Enter config file path (leave empty for default): ");
        var path = System.Console.ReadLine()?.Trim();
        
        try
        {
            _configManager.Save(_options, string.IsNullOrEmpty(path) ? null : path);
            _logger.Info("Configuration saved successfully.");
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to save configuration.", ex);
        }
    }

    private bool ValidatePaths()
    {
        if (string.IsNullOrEmpty(_options.SourcePath))
        {
            _logger.Error("Source path is not set.");
            return false;
        }

        if (string.IsNullOrEmpty(_options.TargetPath))
        {
            _logger.Error("Target path is not set.");
            return false;
        }

        if (!Directory.Exists(_options.SourcePath))
        {
            _logger.Error($"Source directory does not exist: {_options.SourcePath}");
            return false;
        }

        return true;
    }

    private static void DisplaySyncResult(SyncResult result)
    {
        System.Console.WriteLine();
        System.Console.WriteLine("─── Sync Result ───");
        System.Console.WriteLine($"  Status:     {(result.Success ? "Success" : "Failed")}");
        System.Console.WriteLine($"  Copied:     {result.FilesCopied}");
        System.Console.WriteLine($"  Moved:      {result.FilesMoved}");
        System.Console.WriteLine($"  Deleted:    {result.FilesDeleted}");
        System.Console.WriteLine($"  Skipped:    {result.FilesSkipped}");
        System.Console.WriteLine($"  Failed:     {result.FilesFailed}");
        System.Console.WriteLine($"  Transferred:{FormatBytes(result.BytesTransferred)}");
        System.Console.WriteLine($"  Duration:   {result.Duration.TotalSeconds:F2}s");
        System.Console.WriteLine($"  Speed:      {result.SpeedMBps:F2} MB/s");

        if (result.Errors.Count > 0)
        {
            System.Console.WriteLine();
            System.Console.WriteLine("  Errors:");
            foreach (var error in result.Errors.Take(10))
            {
                System.Console.WriteLine($"    - {error}");
            }
            if (result.Errors.Count > 10)
            {
                System.Console.WriteLine($"    ... and {result.Errors.Count - 10} more errors");
            }
        }
        System.Console.WriteLine();
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
        if (_disposed) return;
        _disposed = true;
        
        StopWatchMode();
        _fileWatcher?.Dispose();
    }
}
