using Xunit;
using Synchron.Core;
using Synchron.Core.Interfaces;

namespace Synchron.Core.Tests;

public class FileFilterTests
{
    private readonly ILogger _logger;
    private readonly FileFilter _filter;

    public FileFilterTests()
    {
        _logger = new Logger(LogLevel.None);
        _filter = new FileFilter(_logger);
    }

    [Fact]
    public void IsMatch_NoPatterns_ReturnsTrue()
    {
        var result = _filter.IsMatch("test.txt");
        Assert.True(result);
    }

    [Theory]
    [InlineData("*.txt", "test.txt", true)]
    [InlineData("*.txt", "test.cs", false)]
    [InlineData("*.txt", "folder/test.txt", true)]
    [InlineData("test*", "test123.txt", true)]
    [InlineData("test*", "other.txt", false)]
    public void IsMatch_IncludePattern_ReturnsExpected(string pattern, string filePath, bool expected)
    {
        _filter.AddIncludePattern(pattern);
        var result = _filter.IsMatch(filePath);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("*.tmp", "test.tmp", false)]
    [InlineData("*.tmp", "test.txt", true)]
    [InlineData("*.log", "folder/test.log", false)]
    public void IsMatch_ExcludePattern_ReturnsExpected(string pattern, string filePath, bool expected)
    {
        _filter.AddExcludePattern(pattern);
        var result = _filter.IsMatch(filePath);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void IsMatch_IncludeAndExclude_BothApplied()
    {
        _filter.AddIncludePattern("*.txt");
        _filter.AddExcludePattern("*.tmp");
        
        Assert.True(_filter.IsMatch("test.txt"));
        Assert.False(_filter.IsMatch("test.tmp"));
        Assert.False(_filter.IsMatch("test.cs"));
    }

    [Fact]
    public void ClearPatterns_ResetsFilter()
    {
        _filter.AddIncludePattern("*.txt");
        _filter.ClearPatterns();
        
        Assert.True(_filter.IsMatch("test.txt"));
        Assert.True(_filter.IsMatch("test.cs"));
    }
}
