using Synchron.Core.Interfaces;
using Synchron.Core.Models;

namespace Synchron.Console;

public class CommandLineOptions
{
    public string? SourcePath { get; set; }
    public string? TargetPath { get; set; }
    public SyncMode? Mode { get; set; }
    public List<string> IncludePatterns { get; set; } = new();
    public List<string> ExcludePatterns { get; set; } = new();
    public bool Watch { get; set; }
    public LogLevel? LogLevel { get; set; }
    public string? ConfigPath { get; set; }
    public bool DryRun { get; set; }
    public bool VerifyHash { get; set; }
    public bool Verbose { get; set; }
    public bool ShowHelp { get; set; }
    public bool ShowVersion { get; set; }
    public bool IncludeSubdirectories { get; set; } = true;
    public bool NoSubdirectories { get; set; }
    public ConflictResolution? ConflictResolution { get; set; }
    public int BufferSize { get; set; }
    public string? LogFilePath { get; set; }
    
    public bool NoGitIgnore { get; set; }
    public string? GitIgnoreFile { get; set; }
    public bool ForceGitIgnore { get; set; }
}
