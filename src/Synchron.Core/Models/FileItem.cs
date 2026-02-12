namespace Synchron.Core.Models;

public class FileItem
{
    public string FullPath { get; set; } = string.Empty;
    public string RelativePath { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTime LastWriteTime { get; set; }
    public DateTime CreationTime { get; set; }
    public FileAttributes Attributes { get; set; }
    public string? Hash { get; set; }
    public bool IsDirectory { get; set; }
    
    public static FileItem FromFileInfo(FileInfo info, string basePath)
    {
        var relativePath = GetRelativePath(basePath, info.FullName);
        return new FileItem
        {
            FullPath = info.FullName,
            RelativePath = relativePath,
            Size = info.Length,
            LastWriteTime = info.LastWriteTimeUtc,
            CreationTime = info.CreationTimeUtc,
            Attributes = info.Attributes,
            IsDirectory = false
        };
    }
    
    public static FileItem FromDirectoryInfo(DirectoryInfo info, string basePath)
    {
        var relativePath = GetRelativePath(basePath, info.FullName);
        return new FileItem
        {
            FullPath = info.FullName,
            RelativePath = relativePath,
            Size = 0,
            LastWriteTime = info.LastWriteTimeUtc,
            CreationTime = info.CreationTimeUtc,
            Attributes = info.Attributes,
            IsDirectory = true
        };
    }
    
    private static string GetRelativePath(string basePath, string fullPath)
    {
        var baseUri = new Uri(basePath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar);
        var fullUri = new Uri(fullPath);
        return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fullUri).ToString()
            .Replace('/', Path.DirectorySeparatorChar));
    }
}
