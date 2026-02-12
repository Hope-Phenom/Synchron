namespace Synchron.Core.GitSupport;

public interface IGitIgnoreParser
{
    List<GitIgnoreRule> Parse(string filePath);
    List<GitIgnoreRule> ParseContent(string content, string sourceFile);
    bool IsIgnored(string relativePath, bool isDirectory, IEnumerable<GitIgnoreRule> rules);
}

public interface IGitEnvironmentDetector
{
    GitEnvironmentInfo Detect(string directory);
    GitEnvironmentInfo DetectWithExternalIgnore(string directory, string externalGitIgnorePath);
}

public interface IGitIgnoreRuleCache
{
    List<GitIgnoreRule>? GetRules(string filePath);
    void SetRules(string filePath, List<GitIgnoreRule> rules);
    void Invalidate(string filePath);
    void Clear();
    bool TryGet(string filePath, out List<GitIgnoreRule>? rules);
}

public class GitEnvironmentInfo
{
    public bool IsGitRepository { get; set; }
    public string? GitDirectory { get; set; }
    public string? GitIgnoreFile { get; set; }
    public string? RepositoryRoot { get; set; }
    public List<string> AdditionalGitIgnoreFiles { get; set; } = new();
    public bool HasGitIgnore => !string.IsNullOrEmpty(GitIgnoreFile) || AdditionalGitIgnoreFiles.Count > 0;

    public static GitEnvironmentInfo None => new() { IsGitRepository = false };

    public override string ToString()
    {
        if (!IsGitRepository)
            return "Not a Git repository";

        var parts = new List<string> { $"Git: {GitDirectory}" };
        if (HasGitIgnore)
            parts.Add($"GitIgnore: {GitIgnoreFile ?? string.Join(", ", AdditionalGitIgnoreFiles)}");
        
        return string.Join(", ", parts);
    }
}
