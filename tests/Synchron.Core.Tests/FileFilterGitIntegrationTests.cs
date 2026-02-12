using Xunit;
using Synchron.Core;
using Synchron.Core.Interfaces;
using Synchron.Core.Models;

namespace Synchron.Core.Tests;

public class FileFilterGitIntegrationTests : IDisposable
{
    private readonly string _testRoot;
    private readonly string _sourceDir;

    public FileFilterGitIntegrationTests()
    {
        _testRoot = Path.Combine(Path.GetTempPath(), $"filefilter_git_test_{Guid.NewGuid()}");
        _sourceDir = Path.Combine(_testRoot, "source");
        Directory.CreateDirectory(_sourceDir);
    }

    [Fact]
    public void FileFilter_WithGitIgnoreEnabled_FiltersGitIgnoredFiles()
    {
        var gitDir = Path.Combine(_sourceDir, ".git");
        var gitIgnorePath = Path.Combine(_sourceDir, ".gitignore");
        
        Directory.CreateDirectory(gitDir);
        File.WriteAllText(gitIgnorePath, "*.log\nbin/\n");

        var options = new GitIgnoreOptions
        {
            Enabled = true,
            AutoDetect = true
        };

        using var filter = new FileFilter(new Logger(LogLevel.None), _sourceDir, options);

        Assert.False(filter.IsMatch(Path.Combine(_sourceDir, "test.log"), false));
        Assert.False(filter.IsMatch(Path.Combine(_sourceDir, "bin"), true));
        Assert.True(filter.IsMatch(Path.Combine(_sourceDir, "test.txt"), false));
        Assert.True(filter.IsMatch(Path.Combine(_sourceDir, "src"), true));
    }

    [Fact]
    public void FileFilter_WithGitIgnoreDisabled_DoesNotFilterGitIgnoredFiles()
    {
        var gitDir = Path.Combine(_sourceDir, ".git");
        var gitIgnorePath = Path.Combine(_sourceDir, ".gitignore");
        
        Directory.CreateDirectory(gitDir);
        File.WriteAllText(gitIgnorePath, "*.log\n");

        var options = new GitIgnoreOptions
        {
            Enabled = false,
            AutoDetect = true
        };

        using var filter = new FileFilter(new Logger(LogLevel.None), _sourceDir, options);

        Assert.True(filter.IsMatch(Path.Combine(_sourceDir, "test.log"), false));
    }

    [Fact]
    public void FileFilter_WithExternalGitIgnore_UsesExternalRules()
    {
        var externalIgnorePath = Path.Combine(_testRoot, "external.gitignore");
        File.WriteAllText(externalIgnorePath, "*.tmp\n*.bak\n");

        var options = new GitIgnoreOptions
        {
            Enabled = true,
            AutoDetect = false,
            ExternalGitIgnorePath = externalIgnorePath,
            OverrideAutoDetect = true
        };

        using var filter = new FileFilter(new Logger(LogLevel.None), _sourceDir, options);

        Assert.False(filter.IsMatch(Path.Combine(_sourceDir, "file.tmp"), false));
        Assert.False(filter.IsMatch(Path.Combine(_sourceDir, "file.bak"), false));
        Assert.True(filter.IsMatch(Path.Combine(_sourceDir, "file.txt"), false));
    }

    [Fact]
    public void FileFilter_WithExplicitExclude_AppliesBothRules()
    {
        var gitDir = Path.Combine(_sourceDir, ".git");
        var gitIgnorePath = Path.Combine(_sourceDir, ".gitignore");
        
        Directory.CreateDirectory(gitDir);
        File.WriteAllText(gitIgnorePath, "*.log\n");

        var options = new GitIgnoreOptions
        {
            Enabled = true,
            AutoDetect = true
        };

        using var filter = new FileFilter(new Logger(LogLevel.None), _sourceDir, options);
        filter.AddExcludePattern("*.tmp");

        Assert.False(filter.IsMatch(Path.Combine(_sourceDir, "test.log"), false));
        Assert.False(filter.IsMatch(Path.Combine(_sourceDir, "test.tmp"), false));
        Assert.True(filter.IsMatch(Path.Combine(_sourceDir, "test.txt"), false));
    }

    [Fact]
    public void FileFilter_NoGitEnvironment_WorksWithExplicitPatterns()
    {
        var options = new GitIgnoreOptions
        {
            Enabled = true,
            AutoDetect = true
        };

        using var filter = new FileFilter(new Logger(LogLevel.None), _sourceDir, options);
        filter.AddExcludePattern("*.tmp");

        Assert.False(filter.IsMatch(Path.Combine(_sourceDir, "test.tmp"), false));
        Assert.True(filter.IsMatch(Path.Combine(_sourceDir, "test.txt"), false));
    }

    public void Dispose()
    {
        if (Directory.Exists(_testRoot))
        {
            try
            {
                Directory.Delete(_testRoot, true);
            }
            catch
            {
            }
        }
    }
}
