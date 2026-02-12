using Xunit;
using Synchron.Core.GitSupport;
using Synchron.Core.Interfaces;

namespace Synchron.Core.Tests;

public class GitIgnoreParserTests
{
    private readonly GitIgnoreParser _parser;

    public GitIgnoreParserTests()
    {
        _parser = new GitIgnoreParser(new Logger(LogLevel.None));
    }

    [Fact]
    public void ParseContent_EmptyContent_ReturnsEmptyList()
    {
        var rules = _parser.ParseContent("", "test.gitignore");
        Assert.Empty(rules);
    }

    [Fact]
    public void ParseContent_CommentLines_AreIgnored()
    {
        var content = "# This is a comment\n# Another comment\n";
        var rules = _parser.ParseContent(content, "test.gitignore");
        Assert.Empty(rules);
    }

    [Fact]
    public void ParseContent_SimplePattern_ParsesCorrectly()
    {
        var content = "*.txt\n";
        var rules = _parser.ParseContent(content, "test.gitignore");
        
        Assert.Single(rules);
        Assert.Equal("*.txt", rules[0].Pattern);
        Assert.False(rules[0].IsNegation);
        Assert.False(rules[0].IsDirectoryOnly);
    }

    [Fact]
    public void ParseContent_NegationPattern_ParsesCorrectly()
    {
        var content = "!important.txt\n";
        var rules = _parser.ParseContent(content, "test.gitignore");
        
        Assert.Single(rules);
        Assert.True(rules[0].IsNegation);
        Assert.Equal("important.txt", rules[0].Pattern);
    }

    [Fact]
    public void ParseContent_DirectoryPattern_ParsesCorrectly()
    {
        var content = "node_modules/\n";
        var rules = _parser.ParseContent(content, "test.gitignore");
        
        Assert.Single(rules);
        Assert.True(rules[0].IsDirectoryOnly);
        Assert.Equal("node_modules", rules[0].Pattern);
    }

    [Fact]
    public void ParseContent_AnchoredPattern_ParsesCorrectly()
    {
        var content = "/root-file.txt\n";
        var rules = _parser.ParseContent(content, "test.gitignore");
        
        Assert.Single(rules);
        Assert.True(rules[0].IsAnchored);
        Assert.Equal("root-file.txt", rules[0].Pattern);
    }

    [Fact]
    public void ParseContent_MultiplePatterns_ParsesAll()
    {
        var content = "*.txt\n*.log\nbin/\n";
        var rules = _parser.ParseContent(content, "test.gitignore");
        
        Assert.Equal(3, rules.Count);
    }

    [Fact]
    public void IsIgnored_SimpleWildcard_MatchesCorrectly()
    {
        var content = "*.txt\n";
        var rules = _parser.ParseContent(content, "test.gitignore");
        
        Assert.True(_parser.IsIgnored("test.txt", false, rules));
        Assert.True(_parser.IsIgnored("folder/test.txt", false, rules));
        Assert.False(_parser.IsIgnored("test.cs", false, rules));
    }

    [Fact]
    public void IsIgnored_DirectoryOnly_OnlyMatchesDirectories()
    {
        var content = "node_modules/\n";
        var rules = _parser.ParseContent(content, "test.gitignore");
        
        Assert.True(_parser.IsIgnored("node_modules", true, rules));
        Assert.False(_parser.IsIgnored("node_modules", false, rules));
    }

    [Fact]
    public void IsIgnored_NegationRule_OverridesPreviousRules()
    {
        var content = "*.txt\n!important.txt\n";
        var rules = _parser.ParseContent(content, "test.gitignore");
        
        Assert.True(_parser.IsIgnored("test.txt", false, rules));
        Assert.False(_parser.IsIgnored("important.txt", false, rules));
    }

    [Fact]
    public void IsIgnored_DoubleAsterisk_MatchesMultipleDirectories()
    {
        var content = "**/temp/**\n";
        var rules = _parser.ParseContent(content, "test.gitignore");
        
        Assert.True(_parser.IsIgnored("temp/file.txt", false, rules));
        Assert.True(_parser.IsIgnored("a/temp/b/file.txt", false, rules));
    }

    [Fact]
    public void IsIgnored_QuestionMark_MatchesSingleCharacter()
    {
        var content = "file?.txt\n";
        var rules = _parser.ParseContent(content, "test.gitignore");
        
        Assert.True(_parser.IsIgnored("file1.txt", false, rules));
        Assert.True(_parser.IsIgnored("fileA.txt", false, rules));
        Assert.False(_parser.IsIgnored("file.txt", false, rules));
        Assert.False(_parser.IsIgnored("file12.txt", false, rules));
    }

    [Fact]
    public void IsIgnored_AnchoredPattern_OnlyMatchesAtRoot()
    {
        var content = "/root.txt\n";
        var rules = _parser.ParseContent(content, "test.gitignore");
        
        Assert.True(_parser.IsIgnored("root.txt", false, rules));
        Assert.False(_parser.IsIgnored("sub/root.txt", false, rules));
    }

    [Fact]
    public void ParseContent_EscapedHash_ParsesCorrectly()
    {
        var content = "\\#hashfile.txt\n";
        var rules = _parser.ParseContent(content, "test.gitignore");
        
        Assert.Single(rules);
        Assert.Equal("#hashfile.txt", rules[0].Pattern);
    }

    [Fact]
    public void IsIgnored_ComplexGitIgnore_MatchesCorrectly()
    {
        var content = @"
# Build outputs
bin/
obj/
*.dll
*.exe

# IDE
.vs/
.idea/
*.user
*.suo

# Logs
*.log
logs/

# Exceptions
!important.dll
";
        var rules = _parser.ParseContent(content, "test.gitignore");
        
        Assert.True(_parser.IsIgnored("bin", true, rules));
        Assert.True(_parser.IsIgnored("obj", true, rules));
        Assert.True(_parser.IsIgnored("test.dll", false, rules));
        Assert.True(_parser.IsIgnored("test.exe", false, rules));
        Assert.True(_parser.IsIgnored(".vs", true, rules));
        Assert.True(_parser.IsIgnored("debug.log", false, rules));
        Assert.False(_parser.IsIgnored("important.dll", false, rules));
        Assert.False(_parser.IsIgnored("test.cs", false, rules));
    }
}
