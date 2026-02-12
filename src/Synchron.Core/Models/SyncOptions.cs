using Synchron.Core.Interfaces;

namespace Synchron.Core.Models;

public class SyncOptions
{
    public string SourcePath { get; set; } = string.Empty;
    public string TargetPath { get; set; } = string.Empty;
    public SyncMode Mode { get; set; } = SyncMode.Diff;
    public ConflictResolution ConflictResolution { get; set; } = ConflictResolution.OverwriteIfNewer;
    public CompareMethod CompareMethod { get; set; } = CompareMethod.TimestampAndSize;
    public List<string> IncludePatterns { get; set; } = new();
    public List<string> ExcludePatterns { get; set; } = new();
    public bool IncludeSubdirectories { get; set; } = true;
    public bool DryRun { get; set; }
    public bool VerifyHash { get; set; }
    public bool PreserveTimestamps { get; set; } = true;
    public bool PreserveAttributes { get; set; } = true;
    public int BufferSize { get; set; } = 1024 * 1024;
    public int MaxRetries { get; set; } = 3;
    public int RetryDelayMs { get; set; } = 1000;
    public LogLevel LogLevel { get; set; } = LogLevel.Info;
    public string? LogFilePath { get; set; }
    public bool WatchMode { get; set; }
    public int WatchDebounceMs { get; set; } = 500;
    public GitIgnoreOptions GitIgnore { get; set; } = new();
}

public enum SyncMode
{
    Diff,
    Sync,
    Move,
    Mirror
}

public enum ConflictResolution
{
    Overwrite,
    OverwriteIfNewer,
    Skip,
    Rename,
    Ask
}

public enum CompareMethod
{
    SizeOnly,
    TimestampOnly,
    TimestampAndSize,
    Hash
}
