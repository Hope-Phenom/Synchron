using System.Text.RegularExpressions;
using Synchron.Core.GitSupport;
using Synchron.Core.Interfaces;
using Synchron.Core.Models;

namespace Synchron.Core;

public sealed class FileFilter : IFileFilter, IDisposable
{
    private readonly List<Regex> _includePatterns = new();
    private readonly List<Regex> _excludePatterns = new();
    private readonly ILogger _logger;
    
    private readonly GitIgnoreParser? _gitIgnoreParser;
    private readonly GitEnvironmentDetector? _gitDetector;
    private readonly GitIgnoreRuleCache? _ruleCache;
    private readonly GitIgnoreOptions? _gitIgnoreOptions;
    private readonly string? _sourcePath;
    
    private List<GitIgnoreRule> _gitIgnoreRules = new();
    private string? _loadedGitIgnorePath;
    private bool _disposed;

    public FileFilter(ILogger logger)
    {
        _logger = logger;
    }

    public FileFilter(
        ILogger logger,
        string sourcePath,
        GitIgnoreOptions gitIgnoreOptions) : this(logger)
    {
        _sourcePath = sourcePath;
        _gitIgnoreOptions = gitIgnoreOptions;
        _gitIgnoreParser = new GitIgnoreParser(logger);
        _gitDetector = new GitEnvironmentDetector(logger);
        _ruleCache = new GitIgnoreRuleCache(logger);
        
        InitializeGitIgnore();
    }

    private void InitializeGitIgnore()
    {
        if (_gitIgnoreOptions == null || !_gitIgnoreOptions.Enabled)
        {
            _logger.Debug("GitIgnore filtering is disabled");
            return;
        }

        if (!string.IsNullOrEmpty(_gitIgnoreOptions.ExternalGitIgnorePath))
        {
            LoadExternalGitIgnore(_gitIgnoreOptions.ExternalGitIgnorePath);
            
            if (_gitIgnoreOptions.OverrideAutoDetect)
            {
                _logger.Info("Using external gitignore with override (auto-detection skipped)");
                return;
            }
        }

        if (_gitIgnoreOptions.AutoDetect && !string.IsNullOrEmpty(_sourcePath))
        {
            DetectAndLoadGitIgnore();
        }
    }

    private void LoadExternalGitIgnore(string path)
    {
        if (!File.Exists(path))
        {
            _logger.Warning($"External gitignore file not found: {path}");
            return;
        }

        _logger.Info($"Loading external gitignore: {path}");
        
        if (_ruleCache!.TryGet(path, out var cachedRules))
        {
            _gitIgnoreRules = cachedRules!;
            _loadedGitIgnorePath = path;
            _logger.Debug($"Using cached rules from external gitignore");
            return;
        }

        var rules = _gitIgnoreParser!.Parse(path);
        _ruleCache.SetRules(path, rules);
        _gitIgnoreRules = rules;
        _loadedGitIgnorePath = path;
    }

    private void DetectAndLoadGitIgnore()
    {
        var envInfo = _gitDetector!.Detect(_sourcePath!);
        
        if (!envInfo.IsGitRepository && !envInfo.HasGitIgnore)
        {
            _logger.Debug("No Git environment or .gitignore detected");
            return;
        }

        if (!string.IsNullOrEmpty(envInfo.GitIgnoreFile))
        {
            LoadGitIgnoreFromPath(envInfo.GitIgnoreFile);
        }

        foreach (var additionalFile in envInfo.AdditionalGitIgnoreFiles)
        {
            LoadGitIgnoreFromPath(additionalFile);
        }
    }

    private void LoadGitIgnoreFromPath(string path)
    {
        if (_loadedGitIgnorePath == path && _gitIgnoreRules.Count > 0)
        {
            return;
        }

        if (_ruleCache!.TryGet(path, out var cachedRules))
        {
            _gitIgnoreRules.AddRange(cachedRules!);
            _logger.Debug($"Using cached rules from: {path}");
            return;
        }

        var rules = _gitIgnoreParser!.Parse(path);
        if (rules.Count > 0)
        {
            _ruleCache.SetRules(path, rules);
            _gitIgnoreRules.AddRange(rules);
            _loadedGitIgnorePath = path;
        }
    }

    public bool IsMatch(string filePath)
    {
        return IsMatch(filePath, false);
    }

    public bool IsMatch(string filePath, bool isDirectory)
    {
        var fileName = Path.GetFileName(filePath);
        var normalizedPath = filePath.Replace('\\', '/');

        if (_gitIgnoreRules.Count > 0 && !string.IsNullOrEmpty(_sourcePath) && _gitIgnoreParser != null)
        {
            var relativePath = GetRelativePath(_sourcePath, filePath);
            
            if (_gitIgnoreParser.IsIgnored(relativePath, isDirectory, _gitIgnoreRules))
            {
                _logger.Debug($"File excluded by gitignore: {filePath}");
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

        if (_includePatterns.Count > 0)
        {
            var included = _includePatterns.Any(p => p.IsMatch(normalizedPath) || p.IsMatch(fileName));
            if (!included)
            {
                _logger.Debug($"File excluded (not in include patterns): {filePath}");
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

    public void AddGitIgnoreRules(List<GitIgnoreRule> rules)
    {
        _gitIgnoreRules.AddRange(rules);
        _logger.Debug($"Added {rules.Count} gitignore rules");
    }

    public void ClearGitIgnoreRules()
    {
        _gitIgnoreRules.Clear();
        _logger.Debug("All gitignore rules cleared");
    }

    public int GitIgnoreRuleCount => _gitIgnoreRules.Count;

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

    private static string GetRelativePath(string basePath, string fullPath)
    {
        if (Path.IsPathRooted(fullPath))
        {
            var baseUri = new Uri(basePath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar);
            var fullUri = new Uri(fullPath);
            return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fullUri).ToString()
                .Replace('/', Path.DirectorySeparatorChar));
        }
        
        return fullPath.Replace('\\', '/').TrimStart('/');
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        
        _ruleCache?.Dispose();
    }
}
