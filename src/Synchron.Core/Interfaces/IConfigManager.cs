using Synchron.Core.Models;

namespace Synchron.Core.Interfaces;

public interface IFileFilter
{
    bool IsMatch(string filePath);
    bool IsMatch(string filePath, bool isDirectory);
    void AddIncludePattern(string pattern);
    void AddExcludePattern(string pattern);
    void ClearPatterns();
}

public interface IConfigManager
{
    SyncOptions Load(string? configPath = null);
    void Save(SyncOptions options, string? configPath = null);
    bool Validate(SyncOptions options);
}
