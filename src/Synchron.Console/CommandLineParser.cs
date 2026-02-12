using System.Text;
using Synchron.Core.Interfaces;
using Synchron.Core.Models;

namespace Synchron.Console;

public static class CommandLineParser
{
    private static readonly string Version = "1.0.0";

    public static CommandLineOptions Parse(string[] args)
    {
        var options = new CommandLineOptions();

        for (int i = 0; i < args.Length; i++)
        {
            var arg = args[i];

            if (arg.StartsWith('-') || arg.StartsWith('/'))
            {
                var (key, value) = ParseArgument(arg);

                switch (key.ToLowerInvariant())
                {
                    case "h":
                    case "help":
                    case "?":
                        options.ShowHelp = true;
                        break;

                    case "v":
                    case "version":
                        options.ShowVersion = true;
                        break;

                    case "m":
                    case "mode":
                        options.Mode = ParseSyncMode(value ?? GetNextArg(args, ref i));
                        break;

                    case "f":
                    case "filter":
                    case "include":
                        options.IncludePatterns.Add(value ?? GetNextArg(args, ref i));
                        break;

                    case "e":
                    case "exclude":
                        options.ExcludePatterns.Add(value ?? GetNextArg(args, ref i));
                        break;

                    case "w":
                    case "watch":
                        options.Watch = true;
                        break;

                    case "l":
                    case "log":
                    case "loglevel":
                        options.LogLevel = ParseLogLevel(value ?? GetNextArg(args, ref i));
                        break;

                    case "c":
                    case "config":
                        options.ConfigPath = value ?? GetNextArg(args, ref i);
                        break;

                    case "dry-run":
                    case "dryrun":
                        options.DryRun = true;
                        break;

                    case "verify":
                        options.VerifyHash = true;
                        break;

                    case "verbose":
                        options.Verbose = true;
                        break;

                    case "s":
                    case "subdirs":
                        options.IncludeSubdirectories = true;
                        break;

                    case "no-subdirs":
                    case "nosubdirs":
                        options.NoSubdirectories = true;
                        break;

                    case "conflict":
                        options.ConflictResolution = ParseConflictResolution(value ?? GetNextArg(args, ref i));
                        break;

                    case "buffer":
                    case "buffersize":
                        if (int.TryParse(value ?? GetNextArg(args, ref i), out var bufferSize))
                        {
                            options.BufferSize = bufferSize;
                        }
                        break;

                    case "logfile":
                        options.LogFilePath = value ?? GetNextArg(args, ref i);
                        break;

                    default:
                        System.Console.WriteLine($"Unknown option: {arg}");
                        break;
                }
            }
            else
            {
                if (string.IsNullOrEmpty(options.SourcePath))
                {
                    options.SourcePath = arg;
                }
                else if (string.IsNullOrEmpty(options.TargetPath))
                {
                    options.TargetPath = arg;
                }
            }
        }

        return options;
    }

    private static (string key, string? value) ParseArgument(string arg)
    {
        var cleanArg = arg.TrimStart('-', '/');
        var separatorIndex = cleanArg.IndexOf(':');

        if (separatorIndex > 0)
        {
            return (cleanArg[..separatorIndex], cleanArg[(separatorIndex + 1)..]);
        }

        return (cleanArg, null);
    }

    private static string? GetNextArg(string[] args, ref int index)
    {
        if (index + 1 < args.Length && !args[index + 1].StartsWith('-') && !args[index + 1].StartsWith('/'))
        {
            index++;
            return args[index];
        }
        return null;
    }

    private static SyncMode ParseSyncMode(string? value)
    {
        return value?.ToLowerInvariant() switch
        {
            "diff" => SyncMode.Diff,
            "sync" => SyncMode.Sync,
            "move" => SyncMode.Move,
            "mirror" => SyncMode.Mirror,
            _ => SyncMode.Diff
        };
    }

    private static LogLevel ParseLogLevel(string? value)
    {
        return value?.ToLowerInvariant() switch
        {
            "debug" => LogLevel.Debug,
            "info" => LogLevel.Info,
            "warn" or "warning" => LogLevel.Warning,
            "error" => LogLevel.Error,
            "none" => LogLevel.None,
            _ => LogLevel.Info
        };
    }

    private static ConflictResolution ParseConflictResolution(string? value)
    {
        return value?.ToLowerInvariant() switch
        {
            "overwrite" => ConflictResolution.Overwrite,
            "newer" => ConflictResolution.OverwriteIfNewer,
            "skip" => ConflictResolution.Skip,
            "rename" => ConflictResolution.Rename,
            "ask" => ConflictResolution.Ask,
            _ => ConflictResolution.OverwriteIfNewer
        };
    }

    public static void ShowHelp()
    {
        var help = new StringBuilder();
        help.AppendLine();
        help.AppendLine("Synchron - Fast File Synchronization Tool");
        help.AppendLine($"Version: {Version}");
        help.AppendLine();
        help.AppendLine("Usage: Synchron <source> <target> [options]");
        help.AppendLine();
        help.AppendLine("Arguments:");
        help.AppendLine("  <source>       Source directory path");
        help.AppendLine("  <target>       Target directory path");
        help.AppendLine();
        help.AppendLine("Options:");
        help.AppendLine("  -m, --mode <mode>       Sync mode: diff, sync, move, mirror (default: diff)");
        help.AppendLine("  -f, --filter <pattern>  Include file pattern (e.g., *.txt, **/*.cs)");
        help.AppendLine("  -e, --exclude <pattern> Exclude file pattern");
        help.AppendLine("  -w, --watch             Enable real-time watch mode");
        help.AppendLine("  -l, --log <level>       Log level: debug, info, warn, error (default: info)");
        help.AppendLine("  -c, --config <file>     Configuration file path");
        help.AppendLine("      --dry-run           Preview changes without executing");
        help.AppendLine("      --verify            Use hash verification for file comparison");
        help.AppendLine("      --verbose           Show detailed output");
        help.AppendLine("      --no-subdirs        Do not include subdirectories");
        help.AppendLine("      --conflict <mode>   Conflict resolution: overwrite, newer, skip, rename");
        help.AppendLine("      --buffer <size>     Buffer size in bytes (default: 1MB)");
        help.AppendLine("      --logfile <path>    Write logs to file");
        help.AppendLine("  -v, --version           Show version information");
        help.AppendLine("  -h, --help              Show this help message");
        help.AppendLine();
        help.AppendLine("Sync Modes:");
        help.AppendLine("  diff    Copy new and changed files only (default)");
        help.AppendLine("  sync    Same as diff, ensure target matches source");
        help.AppendLine("  move    Move files from source to target");
        help.AppendLine("  mirror  Make target an exact copy of source (delete extra files)");
        help.AppendLine();
        help.AppendLine("Examples:");
        help.AppendLine("  Synchron C:\\Source D:\\Backup");
        help.AppendLine("  Synchron C:\\Source D:\\Backup -m mirror --dry-run");
        help.AppendLine("  Synchron C:\\Source D:\\Backup -f *.txt -e *.tmp");
        help.AppendLine("  Synchron C:\\Source D:\\Backup -w -l debug");
        help.AppendLine();

        System.Console.WriteLine(help.ToString());
    }

    public static void ShowVersion()
    {
        System.Console.WriteLine($"Synchron version {Version}");
    }
}
