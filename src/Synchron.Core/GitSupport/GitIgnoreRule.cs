using System.Text.RegularExpressions;

namespace Synchron.Core.GitSupport;

public sealed class GitIgnoreRule
{
    public string Pattern { get; }
    public bool IsNegation { get; }
    public bool IsDirectoryOnly { get; }
    public bool IsAnchored { get; }
    public Regex? CompiledRegex { get; }
    public string SourceFile { get; }
    public int LineNumber { get; }

    public GitIgnoreRule(
        string pattern,
        bool isNegation,
        bool isDirectoryOnly,
        bool isAnchored,
        Regex? compiledRegex,
        string sourceFile,
        int lineNumber)
    {
        Pattern = pattern;
        IsNegation = isNegation;
        IsDirectoryOnly = isDirectoryOnly;
        IsAnchored = isAnchored;
        CompiledRegex = compiledRegex;
        SourceFile = sourceFile;
        LineNumber = lineNumber;
    }

    public bool Matches(string relativePath, bool isDirectory)
    {
        if (CompiledRegex is null)
            return false;

        if (IsDirectoryOnly && !isDirectory)
            return false;

        return CompiledRegex.IsMatch(relativePath);
    }

    public override string ToString()
    {
        var prefix = IsNegation ? "!" : "";
        var suffix = IsDirectoryOnly ? "/" : "";
        return $"{prefix}{Pattern}{suffix}";
    }
}
