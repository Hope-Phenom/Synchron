using Synchron.Core.Models;

namespace Synchron.Core.Interfaces;

public interface ISyncEngine
{
    Task<SyncResult> SyncAsync(SyncOptions options, CancellationToken cancellationToken = default);
    Task<SyncPreview> PreviewAsync(SyncOptions options, CancellationToken cancellationToken = default);
    event EventHandler<SyncProgressEventArgs>? ProgressChanged;
}

public class SyncProgressEventArgs : EventArgs
{
    public string CurrentFile { get; set; } = string.Empty;
    public long TotalFiles { get; set; }
    public long ProcessedFiles { get; set; }
    public long TotalBytes { get; set; }
    public long ProcessedBytes { get; set; }
    public SyncAction Action { get; set; }
}

public enum SyncAction
{
    Copy,
    Move,
    Delete,
    Skip,
    Error
}
