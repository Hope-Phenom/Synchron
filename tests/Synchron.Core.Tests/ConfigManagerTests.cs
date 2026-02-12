using Xunit;
using Synchron.Core;
using Synchron.Core.Interfaces;
using Synchron.Core.Models;

namespace Synchron.Core.Tests;

public class ConfigManagerTests : IDisposable
{
    private readonly ILogger _logger;
    private readonly ConfigManager _configManager;
    private readonly string _testConfigPath;

    public ConfigManagerTests()
    {
        _logger = new Logger(LogLevel.None);
        _configManager = new ConfigManager(_logger);
        _testConfigPath = Path.Combine(Path.GetTempPath(), $"synchron_test_{Guid.NewGuid()}.json");
    }

    [Fact]
    public void Validate_ValidOptions_ReturnsTrue()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"synchron_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        var options = new SyncOptions
        {
            SourcePath = tempDir,
            TargetPath = Path.Combine(Path.GetTempPath(), "target"),
            BufferSize = 1024 * 1024
        };

        var result = _configManager.Validate(options);
        Assert.True(result);

        Directory.Delete(tempDir, true);
    }

    [Fact]
    public void Validate_EmptySource_ReturnsFalse()
    {
        var options = new SyncOptions
        {
            SourcePath = "",
            TargetPath = "C:\\Target"
        };

        var result = _configManager.Validate(options);
        Assert.False(result);
    }

    [Fact]
    public void Validate_NonExistentSource_ReturnsFalse()
    {
        var options = new SyncOptions
        {
            SourcePath = "C:\\NonExistentPath12345",
            TargetPath = "C:\\Target"
        };

        var result = _configManager.Validate(options);
        Assert.False(result);
    }

    [Fact]
    public void SaveAndLoad_RoundTrip_PreservesOptions()
    {
        var tempDir = Path.Combine(Path.GetTempPath(), $"synchron_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(tempDir);

        var options = new SyncOptions
        {
            SourcePath = tempDir,
            TargetPath = "C:\\Target",
            Mode = SyncMode.Mirror,
            BufferSize = 2 * 1024 * 1024,
            IncludeSubdirectories = false,
            DryRun = true,
            IncludePatterns = new List<string> { "*.txt", "*.cs" },
            ExcludePatterns = new List<string> { "*.tmp" }
        };

        _configManager.Save(options, _testConfigPath);
        var loaded = _configManager.Load(_testConfigPath);

        Assert.Equal(options.SourcePath, loaded.SourcePath);
        Assert.Equal(options.TargetPath, loaded.TargetPath);
        Assert.Equal(options.Mode, loaded.Mode);
        Assert.Equal(options.BufferSize, loaded.BufferSize);
        Assert.Equal(options.IncludeSubdirectories, loaded.IncludeSubdirectories);
        Assert.Equal(options.DryRun, loaded.DryRun);
        Assert.Equal(options.IncludePatterns, loaded.IncludePatterns);
        Assert.Equal(options.ExcludePatterns, loaded.ExcludePatterns);

        Directory.Delete(tempDir, true);
    }

    public void Dispose()
    {
        if (File.Exists(_testConfigPath))
        {
            File.Delete(_testConfigPath);
        }
    }
}
