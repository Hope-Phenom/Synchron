using Xunit;
using Synchron.Core.GitSupport;
using Synchron.Core.Interfaces;

namespace Synchron.Core.Tests;

public class GitEnvironmentDetectorTests : IDisposable
{
    private readonly GitEnvironmentDetector _detector;
    private readonly string _testRoot;

    public GitEnvironmentDetectorTests()
    {
        _detector = new GitEnvironmentDetector(new Logger(LogLevel.None));
        _testRoot = Path.Combine(Path.GetTempPath(), $"gitignore_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testRoot);
    }

    [Fact]
    public void Detect_NoGitDirectory_ReturnsNoGitEnvironment()
    {
        var result = _detector.Detect(_testRoot);
        
        Assert.False(result.IsGitRepository);
        Assert.Null(result.GitDirectory);
    }

    [Fact]
    public void Detect_WithGitDirectory_ReturnsGitEnvironment()
    {
        var gitDir = Path.Combine(_testRoot, ".git");
        Directory.CreateDirectory(gitDir);

        var result = _detector.Detect(_testRoot);
        
        Assert.True(result.IsGitRepository);
        Assert.NotNull(result.GitDirectory);
        Assert.Equal(_testRoot, result.RepositoryRoot);
    }

    [Fact]
    public void Detect_WithGitIgnoreFile_FindsGitIgnore()
    {
        var gitIgnorePath = Path.Combine(_testRoot, ".gitignore");
        File.WriteAllText(gitIgnorePath, "*.txt\n");

        var result = _detector.Detect(_testRoot);
        
        Assert.NotNull(result.GitIgnoreFile);
    }

    [Fact]
    public void Detect_GitRepoWithGitIgnore_FindsBoth()
    {
        var gitDir = Path.Combine(_testRoot, ".git");
        var gitIgnorePath = Path.Combine(_testRoot, ".gitignore");
        
        Directory.CreateDirectory(gitDir);
        File.WriteAllText(gitIgnorePath, "*.txt\n");

        var result = _detector.Detect(_testRoot);
        
        Assert.True(result.IsGitRepository);
        Assert.True(result.HasGitIgnore);
        Assert.NotNull(result.GitDirectory);
        Assert.NotNull(result.GitIgnoreFile);
    }

    [Fact]
    public void Detect_GitDirectoryInParent_FindsParentRepo()
    {
        var subDir = Path.Combine(_testRoot, "subfolder");
        Directory.CreateDirectory(subDir);
        
        var gitDir = Path.Combine(_testRoot, ".git");
        Directory.CreateDirectory(gitDir);

        var result = _detector.Detect(subDir);
        
        Assert.True(result.IsGitRepository);
        Assert.Equal(_testRoot, result.RepositoryRoot);
    }

    [Fact]
    public void DetectWithExternalIgnore_ExternalFileSpecified_UsesExternalFile()
    {
        var externalIgnorePath = Path.Combine(_testRoot, "external.gitignore");
        File.WriteAllText(externalIgnorePath, "*.log\n");

        var result = _detector.DetectWithExternalIgnore(_testRoot, externalIgnorePath);
        
        Assert.Equal(Path.GetFullPath(externalIgnorePath), result.GitIgnoreFile);
    }

    [Fact]
    public void DetectWithExternalIgnore_NonExistentFile_ReturnsOriginalInfo()
    {
        var result = _detector.DetectWithExternalIgnore(_testRoot, "nonexistent.gitignore");
        
        Assert.False(result.IsGitRepository);
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
