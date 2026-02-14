using Synchron.Core.Interfaces;

namespace Synchron.Core.Models;

public class SyncTask
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public SyncOptions Options { get; set; } = new();
    public bool Enabled { get; set; } = true;

    public override string ToString()
    {
        var status = Enabled ? "enabled" : "disabled";
        var desc = string.IsNullOrEmpty(Description) ? "" : $" - {Description}";
        return $"[{status}] {Name}{desc}";
    }
}
