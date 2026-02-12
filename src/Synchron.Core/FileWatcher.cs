using System.Collections.Concurrent;
using System.Timers;
using Synchron.Core.Interfaces;
using Synchron.Core.Models;
using Timer = System.Timers.Timer;

namespace Synchron.Core;

public sealed class FileWatcher : IFileWatcher, IDisposable
{
    private readonly ILogger _logger;
    private readonly IFileFilter _fileFilter;
    private readonly SyncEngine _syncEngine;
    private readonly SyncOptions _options;
    private FileSystemWatcher? _watcher;
    private readonly ConcurrentDictionary<string, DateTime> _pendingChanges = new();
    private readonly Timer _debounceTimer;
    private readonly object _lock = new();
    private bool _disposed;
    private bool _isRunning;

    public bool IsRunning => _isRunning;
    public string WatchPath { get; }

    public event EventHandler<FileChangedEventArgs>? FileChanged;

    public FileWatcher(ILogger logger, IFileFilter fileFilter, SyncEngine syncEngine, SyncOptions options)
    {
        _logger = logger;
        _fileFilter = fileFilter;
        _syncEngine = syncEngine;
        _options = options;
        WatchPath = options.SourcePath;

        _debounceTimer = new Timer(options.WatchDebounceMs)
        {
            AutoReset = true
        };
        _debounceTimer.Elapsed += OnDebounceTimerElapsed;
    }

    public void Start()
    {
        lock (_lock)
        {
            if (_isRunning)
            {
                _logger.Warning("File watcher is already running");
                return;
            }

            if (!Directory.Exists(WatchPath))
            {
                _logger.Error($"Watch directory does not exist: {WatchPath}");
                return;
            }

            _watcher = new FileSystemWatcher(WatchPath)
            {
                IncludeSubdirectories = _options.IncludeSubdirectories,
                NotifyFilter = NotifyFilters.FileName | NotifyFilters.DirectoryName | 
                               NotifyFilters.LastWrite | NotifyFilters.Size | NotifyFilters.CreationTime,
                InternalBufferSize = 64 * 1024
            };

            _watcher.Created += OnFileCreated;
            _watcher.Changed += OnFileChanged;
            _watcher.Deleted += OnFileDeleted;
            _watcher.Renamed += OnFileRenamed;
            _watcher.Error += OnWatcherError;

            _watcher.EnableRaisingEvents = true;
            _debounceTimer.Start();
            _isRunning = true;

            _logger.Info($"File watcher started on: {WatchPath}");
        }
    }

    public void Stop()
    {
        lock (_lock)
        {
            if (!_isRunning) return;

            _watcher?.Dispose();
            _watcher = null;
            _debounceTimer.Stop();
            _pendingChanges.Clear();
            _isRunning = false;

            _logger.Info("File watcher stopped");
        }
    }

    private void OnFileCreated(object sender, FileSystemEventArgs e)
    {
        if (!_fileFilter.IsMatch(e.FullPath)) return;
        
        _logger.Debug($"File created: {e.FullPath}");
        QueueChange(e.FullPath, FileChangeType.Created);
    }

    private void OnFileChanged(object sender, FileSystemEventArgs e)
    {
        if (!_fileFilter.IsMatch(e.FullPath)) return;
        
        _logger.Debug($"File changed: {e.FullPath}");
        QueueChange(e.FullPath, FileChangeType.Changed);
    }

    private void OnFileDeleted(object sender, FileSystemEventArgs e)
    {
        if (!_fileFilter.IsMatch(e.FullPath)) return;
        
        _logger.Debug($"File deleted: {e.FullPath}");
        QueueChange(e.FullPath, FileChangeType.Deleted);
    }

    private void OnFileRenamed(object sender, RenamedEventArgs e)
    {
        if (!_fileFilter.IsMatch(e.FullPath)) return;
        
        _logger.Debug($"File renamed: {e.OldFullPath} -> {e.FullPath}");
        QueueChange(e.OldFullPath, FileChangeType.Deleted);
        QueueChange(e.FullPath, FileChangeType.Created);
    }

    private void OnWatcherError(object sender, ErrorEventArgs e)
    {
        _logger.Error("File watcher error", e.GetException());
        
        if (e.GetException() is InternalBufferOverflowException)
        {
            _logger.Warning("File watcher buffer overflow, some events may have been missed");
        }
    }

    private void QueueChange(string filePath, FileChangeType changeType)
    {
        var now = DateTime.UtcNow;
        _pendingChanges.AddOrUpdate(filePath, now, (_, _) => now);
    }

    private void OnDebounceTimerElapsed(object? sender, ElapsedEventArgs e)
    {
        if (_pendingChanges.IsEmpty) return;

        var now = DateTime.UtcNow;
        var threshold = now.AddMilliseconds(-_options.WatchDebounceMs);
        var filesToProcess = new List<string>();

        foreach (var kvp in _pendingChanges)
        {
            if (kvp.Value < threshold)
            {
                _pendingChanges.TryRemove(kvp.Key, out _);
                filesToProcess.Add(kvp.Key);
            }
        }

        if (filesToProcess.Count > 0)
        {
            _logger.Debug($"Processing {filesToProcess.Count} pending file changes");
            _ = ProcessChangesAsync(filesToProcess);
        }
    }

    private async Task ProcessChangesAsync(List<string> files)
    {
        try
        {
            _logger.Info($"Starting sync for {files.Count} changed files");
            var result = await _syncEngine.SyncAsync(_options);

            if (result.Success)
            {
                _logger.Info($"Auto-sync completed: {result.FilesCopied} copied, {result.FilesDeleted} deleted");
            }
            else
            {
                _logger.Error($"Auto-sync failed with {result.Errors.Count} errors");
            }

            foreach (var file in files)
            {
                FileChanged?.Invoke(this, new FileChangedEventArgs
                {
                    FilePath = file,
                    ChangeType = FileChangeType.Changed
                });
            }
        }
        catch (Exception ex)
        {
            _logger.Error("Failed to process file changes", ex);
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
        }

        Stop();
        _debounceTimer.Dispose();
    }
}
