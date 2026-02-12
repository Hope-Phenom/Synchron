using System.Collections.Concurrent;
using Synchron.Core.Interfaces;

namespace Synchron.Core.GitSupport;

public sealed class GitIgnoreRuleCache : IGitIgnoreRuleCache, IDisposable
{
    private readonly ILogger _logger;
    private readonly ConcurrentDictionary<string, CacheEntry> _cache = new();
    private readonly Timer? _cleanupTimer;
    private readonly TimeSpan _cacheExpiration = TimeSpan.FromMinutes(30);
    private readonly object _lock = new();
    private bool _disposed;

    public GitIgnoreRuleCache(ILogger logger)
    {
        _logger = logger;
        _cleanupTimer = new Timer(CleanupExpiredEntries, null, TimeSpan.FromMinutes(5), TimeSpan.FromMinutes(5));
    }

    public List<GitIgnoreRule>? GetRules(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return null;

        var normalizedPath = Path.GetFullPath(filePath);

        if (_cache.TryGetValue(normalizedPath, out var entry))
        {
            if (IsEntryValid(entry))
            {
                _logger.Debug($"Cache hit for gitignore: {filePath}");
                return entry.Rules;
            }

            _cache.TryRemove(normalizedPath, out _);
            _logger.Debug($"Cache expired for gitignore: {filePath}");
        }

        return null;
    }

    public void SetRules(string filePath, List<GitIgnoreRule> rules)
    {
        if (string.IsNullOrEmpty(filePath) || rules == null)
            return;

        var normalizedPath = Path.GetFullPath(filePath);
        var lastWriteTime = File.GetLastWriteTimeUtc(filePath);

        var entry = new CacheEntry
        {
            Rules = rules,
            LastWriteTime = lastWriteTime,
            CachedAt = DateTime.UtcNow
        };

        _cache.AddOrUpdate(normalizedPath, entry, (_, _) => entry);
        _logger.Debug($"Cached {rules.Count} rules for gitignore: {filePath}");
    }

    public void Invalidate(string filePath)
    {
        if (string.IsNullOrEmpty(filePath))
            return;

        var normalizedPath = Path.GetFullPath(filePath);
        if (_cache.TryRemove(normalizedPath, out _))
        {
            _logger.Debug($"Invalidated cache for gitignore: {filePath}");
        }
    }

    public void Clear()
    {
        var count = _cache.Count;
        _cache.Clear();
        _logger.Info($"Cleared gitignore cache ({count} entries)");
    }

    public bool TryGet(string filePath, out List<GitIgnoreRule>? rules)
    {
        rules = GetRules(filePath);
        return rules != null;
    }

    private bool IsEntryValid(CacheEntry entry)
    {
        if (DateTime.UtcNow - entry.CachedAt > _cacheExpiration)
            return false;

        if (!File.Exists(entry.Rules?.FirstOrDefault()?.SourceFile ?? ""))
            return true;

        var currentWriteTime = File.GetLastWriteTimeUtc(entry.Rules!.First().SourceFile);
        return currentWriteTime == entry.LastWriteTime;
    }

    private void CleanupExpiredEntries(object? state)
    {
        var expiredKeys = new List<string>();

        foreach (var kvp in _cache)
        {
            if (!IsEntryValid(kvp.Value))
            {
                expiredKeys.Add(kvp.Key);
            }
        }

        foreach (var key in expiredKeys)
        {
            _cache.TryRemove(key, out _);
        }

        if (expiredKeys.Count > 0)
        {
            _logger.Debug($"Cleaned up {expiredKeys.Count} expired cache entries");
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
        }

        _cleanupTimer?.Dispose();
        _cache.Clear();
    }

    private sealed class CacheEntry
    {
        public List<GitIgnoreRule>? Rules { get; set; }
        public DateTime LastWriteTime { get; set; }
        public DateTime CachedAt { get; set; }
    }
}
