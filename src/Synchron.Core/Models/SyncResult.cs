using Synchron.Core.Interfaces;

namespace Synchron.Core.Models;

public class SyncResult
{
    public bool Success { get; set; }
    public long FilesCopied { get; set; }
    public long FilesMoved { get; set; }
    public long FilesDeleted { get; set; }
    public long FilesSkipped { get; set; }
    public long FilesFailed { get; set; }
    public long BytesTransferred { get; set; }
    public TimeSpan Duration { get; set; }
    public List<FileOperationResult> Operations { get; set; } = new();
    public List<string> Errors { get; set; } = new();
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    public long TotalFilesProcessed => FilesCopied + FilesMoved + FilesDeleted + FilesSkipped + FilesFailed;
    
    public double SpeedMBps => Duration.TotalSeconds > 0 
        ? BytesTransferred / (1024.0 * 1024.0) / Duration.TotalSeconds 
        : 0;
}

public class FileOperationResult
{
    public string SourcePath { get; set; } = string.Empty;
    public string? TargetPath { get; set; }
    public SyncAction Action { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
    public long BytesTransferred { get; set; }
    public TimeSpan Duration { get; set; }
}

public class SyncPreview
{
    public List<FileItem> ToCopy { get; set; } = new();
    public List<FileItem> ToMove { get; set; } = new();
    public List<FileItem> ToDelete { get; set; } = new();
    public List<FileItem> ToSkip { get; set; } = new();
    public long TotalBytes { get; set; }
    public long TotalFiles => ToCopy.Count + ToMove.Count + ToDelete.Count;
}
