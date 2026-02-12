using System.Text.Json;
using Synchron.Core.Interfaces;
using Synchron.Core.Models;

namespace Synchron.Core;

public class ConfigManager : IConfigManager
{
    private static readonly string DefaultConfigFileName = "synchron.json";
    private readonly ILogger _logger;

    public ConfigManager(ILogger logger)
    {
        _logger = logger;
    }

    public SyncOptions Load(string? configPath = null)
    {
        var path = GetConfigPath(configPath);
        
        if (!File.Exists(path))
        {
            _logger.Debug($"Config file not found: {path}, using defaults");
            return new SyncOptions();
        }

        try
        {
            var json = File.ReadAllText(path);
            var options = JsonSerializer.Deserialize<SyncOptions>(json, new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip
            });

            return options ?? new SyncOptions();
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to load config file: {path}", ex);
            return new SyncOptions();
        }
    }

    public void Save(SyncOptions options, string? configPath = null)
    {
        var path = GetConfigPath(configPath);
        
        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var json = JsonSerializer.Serialize(options, new JsonSerializerOptions
            {
                WriteIndented = true,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            File.WriteAllText(path, json);
            _logger.Info($"Config saved to: {path}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to save config file: {path}", ex);
            throw;
        }
    }

    public bool Validate(SyncOptions options)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(options.SourcePath))
        {
            errors.Add("Source path is required");
        }
        else if (!Directory.Exists(options.SourcePath))
        {
            errors.Add($"Source path does not exist: {options.SourcePath}");
        }

        if (string.IsNullOrWhiteSpace(options.TargetPath))
        {
            errors.Add("Target path is required");
        }

        if (options.BufferSize < 1024)
        {
            errors.Add("Buffer size must be at least 1024 bytes");
        }

        if (options.MaxRetries < 0)
        {
            errors.Add("Max retries cannot be negative");
        }

        if (options.WatchDebounceMs < 0)
        {
            errors.Add("Watch debounce cannot be negative");
        }

        foreach (var error in errors)
        {
            _logger.Error(error);
        }

        return errors.Count == 0;
    }

    private static string GetConfigPath(string? configPath)
    {
        if (!string.IsNullOrEmpty(configPath))
        {
            return Path.GetFullPath(configPath);
        }

        var appDataPath = Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData);
        var synchronPath = Path.Combine(appDataPath, "Synchron");
        return Path.Combine(synchronPath, DefaultConfigFileName);
    }
}
