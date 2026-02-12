using Synchron.Core.Interfaces;

namespace Synchron.Core.GitSupport;

public sealed class GitEnvironmentDetector : IGitEnvironmentDetector
{
    private readonly ILogger _logger;

    public GitEnvironmentDetector(ILogger logger)
    {
        _logger = logger;
    }

    public GitEnvironmentInfo Detect(string directory)
    {
        if (!Directory.Exists(directory))
        {
            _logger.Warning($"Directory does not exist: {directory}");
            return GitEnvironmentInfo.None;
        }

        var currentDir = Path.GetFullPath(directory);
        string? gitDir = null;
        string? gitIgnoreFile = null;
        string? repoRoot = null;

        while (currentDir != null)
        {
            var potentialGitDir = Path.Combine(currentDir, ".git");
            var potentialGitIgnore = Path.Combine(currentDir, ".gitignore");

            if (Directory.Exists(potentialGitDir) || File.Exists(potentialGitDir))
            {
                gitDir = potentialGitDir;
                repoRoot = currentDir;
                _logger.Debug($"Found Git repository at: {currentDir}");

                if (File.Exists(potentialGitIgnore))
                {
                    gitIgnoreFile = potentialGitIgnore;
                    _logger.Debug($"Found .gitignore at: {potentialGitIgnore}");
                }

                break;
            }

            if (gitIgnoreFile == null && File.Exists(potentialGitIgnore))
            {
                gitIgnoreFile = potentialGitIgnore;
                _logger.Debug($"Found .gitignore at: {potentialGitIgnore}");
            }

            var parentDir = Directory.GetParent(currentDir)?.FullName;
            if (parentDir == currentDir)
                break;
            
            currentDir = parentDir;
        }

        var info = new GitEnvironmentInfo
        {
            IsGitRepository = gitDir != null,
            GitDirectory = gitDir,
            GitIgnoreFile = gitIgnoreFile,
            RepositoryRoot = repoRoot
        };

        if (info.IsGitRepository)
        {
            _logger.Info($"Git environment detected: {info}");
        }
        else
        {
            _logger.Debug("No Git environment detected");
        }

        return info;
    }

    public GitEnvironmentInfo DetectWithExternalIgnore(string directory, string externalGitIgnorePath)
    {
        var info = Detect(directory);

        if (string.IsNullOrEmpty(externalGitIgnorePath) || !File.Exists(externalGitIgnorePath))
        {
            _logger.Warning($"External gitignore file not found: {externalGitIgnorePath}");
            return info;
        }

        _logger.Info($"Using external gitignore file: {externalGitIgnorePath}");

        info.GitIgnoreFile = Path.GetFullPath(externalGitIgnorePath);
        info.AdditionalGitIgnoreFiles.Clear();

        return info;
    }

    public static GitEnvironmentInfo DetectGitIgnoreInPath(string directory)
    {
        var gitIgnoreFiles = new List<string>();
        var currentDir = Path.GetFullPath(directory);

        while (currentDir != null)
        {
            var potentialGitIgnore = Path.Combine(currentDir, ".gitignore");
            if (File.Exists(potentialGitIgnore))
            {
                gitIgnoreFiles.Add(potentialGitIgnore);
            }

            var parentDir = Directory.GetParent(currentDir)?.FullName;
            if (parentDir == currentDir)
                break;
            
            currentDir = parentDir;
        }

        return new GitEnvironmentInfo
        {
            IsGitRepository = false,
            AdditionalGitIgnoreFiles = gitIgnoreFiles
        };
    }
}
