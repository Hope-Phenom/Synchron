namespace Synchron.Core.Interfaces;

public interface IFileWatcher
{
    void Start();
    void Stop();
    bool IsRunning { get; }
    string WatchPath { get; }
    event EventHandler<FileChangedEventArgs>? FileChanged;
}

public class FileChangedEventArgs : EventArgs
{
    public string FilePath { get; set; } = string.Empty;
    public FileChangeType ChangeType { get; set; }
}

public enum FileChangeType
{
    Created,
    Changed,
    Deleted,
    Renamed
}
