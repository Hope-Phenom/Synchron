using Xunit;
using Synchron.Core.GitSupport;
using Synchron.Core.Interfaces;

namespace Synchron.Core.Tests;

public class GitIgnoreRuleCacheTests : IDisposable
{
    private readonly GitIgnoreRuleCache _cache;
    private readonly GitIgnoreParser _parser;
    private readonly string _testRoot;
    private readonly string _testGitIgnorePath;

    public GitIgnoreRuleCacheTests()
    {
        _cache = new GitIgnoreRuleCache(new Logger(LogLevel.None));
        _parser = new GitIgnoreParser(new Logger(LogLevel.None));
        _testRoot = Path.Combine(Path.GetTempPath(), $"gitignore_cache_test_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testRoot);
        _testGitIgnorePath = Path.Combine(_testRoot, ".gitignore");
    }

    [Fact]
    public void GetRules_EmptyCache_ReturnsNull()
    {
        var rules = _cache.GetRules(_testGitIgnorePath);
        Assert.Null(rules);
    }

    [Fact]
    public void SetRules_ThenGet_ReturnsCachedRules()
    {
        File.WriteAllText(_testGitIgnorePath, "*.txt\n");
        var rules = _parser.Parse(_testGitIgnorePath);
        
        _cache.SetRules(_testGitIgnorePath, rules);
        var cachedRules = _cache.GetRules(_testGitIgnorePath);
        
        Assert.NotNull(cachedRules);
        Assert.Equal(rules.Count, cachedRules.Count);
    }

    [Fact]
    public void TryGet_ExistingRules_ReturnsTrue()
    {
        File.WriteAllText(_testGitIgnorePath, "*.txt\n");
        var rules = _parser.Parse(_testGitIgnorePath);
        _cache.SetRules(_testGitIgnorePath, rules);
        
        var found = _cache.TryGet(_testGitIgnorePath, out var cachedRules);
        
        Assert.True(found);
        Assert.NotNull(cachedRules);
    }

    [Fact]
    public void TryGet_NonExistingRules_ReturnsFalse()
    {
        var found = _cache.TryGet(_testGitIgnorePath, out var cachedRules);
        
        Assert.False(found);
        Assert.Null(cachedRules);
    }

    [Fact]
    public void Invalidate_ExistingRules_RemovesFromCache()
    {
        File.WriteAllText(_testGitIgnorePath, "*.txt\n");
        var rules = _parser.Parse(_testGitIgnorePath);
        _cache.SetRules(_testGitIgnorePath, rules);
        
        _cache.Invalidate(_testGitIgnorePath);
        var cachedRules = _cache.GetRules(_testGitIgnorePath);
        
        Assert.Null(cachedRules);
    }

    [Fact]
    public void Clear_RemovesAllRules()
    {
        File.WriteAllText(_testGitIgnorePath, "*.txt\n");
        var rules = _parser.Parse(_testGitIgnorePath);
        _cache.SetRules(_testGitIgnorePath, rules);
        
        _cache.Clear();
        var cachedRules = _cache.GetRules(_testGitIgnorePath);
        
        Assert.Null(cachedRules);
    }

    public void Dispose()
    {
        _cache.Dispose();
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
