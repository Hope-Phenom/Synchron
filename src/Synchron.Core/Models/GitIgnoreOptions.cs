namespace Synchron.Core.Models;

public class GitIgnoreOptions
{
    public bool Enabled { get; set; } = true;
    public bool AutoDetect { get; set; } = true;
    public string? ExternalGitIgnorePath { get; set; }
    public bool OverrideAutoDetect { get; set; }

    public GitIgnoreOptions Clone()
    {
        return new GitIgnoreOptions
        {
            Enabled = Enabled,
            AutoDetect = AutoDetect,
            ExternalGitIgnorePath = ExternalGitIgnorePath,
            OverrideAutoDetect = OverrideAutoDetect
        };
    }

    public override string ToString()
    {
        if (!Enabled)
            return "Disabled";

        var parts = new List<string>();
        
        if (AutoDetect)
            parts.Add("AutoDetect");
        
        if (!string.IsNullOrEmpty(ExternalGitIgnorePath))
            parts.Add($"External: {ExternalGitIgnorePath}");
        
        if (OverrideAutoDetect)
            parts.Add("Override");

        return parts.Count > 0 ? string.Join(", ", parts) : "Enabled (no rules)";
    }
}
