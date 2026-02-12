using System.Collections.Concurrent;
using System.Security.Cryptography;
using System.Text;
using System.Text.RegularExpressions;
using Synchron.Core.Interfaces;

namespace Synchron.Core.GitSupport;

public sealed class GitIgnoreParser : IGitIgnoreParser
{
    private readonly ILogger _logger;

    public GitIgnoreParser(ILogger logger)
    {
        _logger = logger;
    }

    public List<GitIgnoreRule> Parse(string filePath)
    {
        if (!File.Exists(filePath))
        {
            _logger.Warning($"GitIgnore file not found: {filePath}");
            return new List<GitIgnoreRule>();
        }

        try
        {
            var content = File.ReadAllText(filePath);
            return ParseContent(content, filePath);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to parse gitignore file: {filePath}", ex);
            return new List<GitIgnoreRule>();
        }
    }

    public List<GitIgnoreRule> ParseContent(string content, string sourceFile)
    {
        var rules = new List<GitIgnoreRule>();
        var lines = content.Split(new[] { "\r\n", "\n", "\r" }, StringSplitOptions.None);

        for (int i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var lineNumber = i + 1;

            var rule = ParseLine(line, sourceFile, lineNumber);
            if (rule != null)
            {
                rules.Add(rule);
                _logger.Debug($"Parsed gitignore rule: {rule} (line {lineNumber})");
            }
        }

        _logger.Info($"Parsed {rules.Count} gitignore rules from {sourceFile}");
        return rules;
    }

    public bool IsIgnored(string relativePath, bool isDirectory, IEnumerable<GitIgnoreRule> rules)
    {
        var normalizedPath = NormalizePath(relativePath);
        var result = false;

        foreach (var rule in rules)
        {
            if (rule.Matches(normalizedPath, isDirectory))
            {
                result = !rule.IsNegation;
                _logger.Debug($"Path '{relativePath}' {(result ? "ignored" : "un-ignored")} by rule: {rule}");
            }
        }

        return result;
    }

    private GitIgnoreRule? ParseLine(string line, string sourceFile, int lineNumber)
    {
        var trimmedLine = line.Trim();

        if (string.IsNullOrEmpty(trimmedLine))
            return null;

        if (trimmedLine.StartsWith('#'))
            return null;

        if (trimmedLine.StartsWith("\\#"))
            trimmedLine = trimmedLine[1..];

        var isNegation = trimmedLine.StartsWith('!');
        if (isNegation)
            trimmedLine = trimmedLine[1..];

        var isDirectoryOnly = trimmedLine.EndsWith('/');
        if (isDirectoryOnly)
            trimmedLine = trimmedLine[..^1];

        var isAnchored = trimmedLine.StartsWith('/');
        if (isAnchored)
            trimmedLine = trimmedLine[1..];

        if (trimmedLine.Contains("/**/"))
        {
            trimmedLine = trimmedLine.Replace("/**/", "(?:/.*/)?");
        }

        var regex = ConvertToRegex(trimmedLine, isAnchored, isDirectoryOnly);

        return new GitIgnoreRule(
            trimmedLine,
            isNegation,
            isDirectoryOnly,
            isAnchored,
            regex,
            sourceFile,
            lineNumber
        );
    }

    private static Regex? ConvertToRegex(string pattern, bool isAnchored, bool isDirectoryOnly)
    {
        try
        {
            var regexPattern = BuildRegexPattern(pattern, isAnchored, isDirectoryOnly);
            return new Regex(regexPattern, RegexOptions.Compiled | RegexOptions.IgnoreCase);
        }
        catch
        {
            return null;
        }
    }

    private static string BuildRegexPattern(string pattern, bool isAnchored, bool isDirectoryOnly)
    {
        var sb = new StringBuilder();
        
        if (isAnchored)
        {
            sb.Append('^');
        }
        else
        {
            sb.Append("(^|/)");
        }

        var i = 0;
        while (i < pattern.Length)
        {
            var c = pattern[i];

            switch (c)
            {
                case '*':
                    if (i + 1 < pattern.Length && pattern[i + 1] == '*')
                    {
                        if (i + 2 < pattern.Length && pattern[i + 2] == '/')
                        {
                            sb.Append("(?:.*/)?");
                            i += 3;
                            continue;
                        }
                        sb.Append(".*");
                        i += 2;
                        continue;
                    }
                    sb.Append("[^/]*");
                    break;

                case '?':
                    sb.Append("[^/]");
                    break;

                case '.':
                case '(':
                case ')':
                case '+':
                case '|':
                case '^':
                case '$':
                case '{':
                case '}':
                case '[':
                case ']':
                    sb.Append('\\');
                    sb.Append(c);
                    break;

                case '\\':
                    if (i + 1 < pattern.Length)
                    {
                        sb.Append('\\');
                        sb.Append(pattern[i + 1]);
                        i++;
                    }
                    break;

                default:
                    sb.Append(c);
                    break;
            }

            i++;
        }

        if (isDirectoryOnly)
        {
            sb.Append("(?:/|$)");
        }
        else
        {
            sb.Append("(?:/.*)?$");
        }

        return sb.ToString();
    }

    private static string NormalizePath(string path)
    {
        return path.Replace('\\', '/').TrimStart('/');
    }
}
