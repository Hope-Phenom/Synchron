using System.Text.RegularExpressions;
using Synchron.Core.Interfaces;

namespace Synchron.Core;

public class FileFilter : IFileFilter
{
    private readonly List<Regex> _includePatterns = new();
    private readonly List<Regex> _excludePatterns = new();
    private readonly ILogger _logger;

    public FileFilter(ILogger logger)
    {
        _logger = logger;
    }

    public bool IsMatch(string filePath)
    {
        var fileName = Path.GetFileName(filePath);
        var normalizedPath = filePath.Replace('\\', '/');

        if (_includePatterns.Count > 0)
        {
            var included = _includePatterns.Any(p => p.IsMatch(normalizedPath) || p.IsMatch(fileName));
            if (!included)
            {
                _logger.Debug($"File excluded (not in include patterns): {filePath}");
                return false;
            }
        }

        if (_excludePatterns.Count > 0)
        {
            var excluded = _excludePatterns.Any(p => p.IsMatch(normalizedPath) || p.IsMatch(fileName));
            if (excluded)
            {
                _logger.Debug($"File excluded (matches exclude pattern): {filePath}");
                return false;
            }
        }

        return true;
    }

    public void AddIncludePattern(string pattern)
    {
        try
        {
            var regex = WildcardToRegex(pattern);
            _includePatterns.Add(regex);
            _logger.Debug($"Added include pattern: {pattern}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Invalid include pattern: {pattern}", ex);
        }
    }

    public void AddExcludePattern(string pattern)
    {
        try
        {
            var regex = WildcardToRegex(pattern);
            _excludePatterns.Add(regex);
            _logger.Debug($"Added exclude pattern: {pattern}");
        }
        catch (Exception ex)
        {
            _logger.Error($"Invalid exclude pattern: {pattern}", ex);
        }
    }

    public void ClearPatterns()
    {
        _includePatterns.Clear();
        _excludePatterns.Clear();
        _logger.Debug("All filter patterns cleared");
    }

    private static Regex WildcardToRegex(string pattern)
    {
        var normalizedPattern = pattern.Replace('\\', '/');
        
        var regexPattern = "^" + Regex.Escape(normalizedPattern)
            .Replace(@"\*\*/", ".*")
            .Replace(@"\*\*", ".*")
            .Replace(@"\*", "[^/]*")
            .Replace(@"\?", "[^/]?")
            + "$";

        return new Regex(regexPattern, RegexOptions.IgnoreCase | RegexOptions.Compiled);
    }
}
