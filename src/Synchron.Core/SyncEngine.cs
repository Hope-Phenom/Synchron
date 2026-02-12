using System.Security.Cryptography;
using Synchron.Core.Interfaces;
using Synchron.Core.Models;

namespace Synchron.Core;

public sealed class SyncEngine : ISyncEngine, IDisposable
{
    private readonly ILogger _logger;
    private readonly IFileFilter _fileFilter;
    private readonly ConfigManager _configManager;
    private readonly object _lock = new();
    private bool _disposed;

    public event EventHandler<SyncProgressEventArgs>? ProgressChanged;

    public SyncEngine(ILogger logger, IFileFilter fileFilter, ConfigManager configManager)
    {
        _logger = logger;
        _fileFilter = fileFilter;
        _configManager = configManager;
    }

    public async Task<SyncPreview> PreviewAsync(SyncOptions options, CancellationToken cancellationToken = default)
    {
        _logger.Info($"Previewing sync from '{options.SourcePath}' to '{options.TargetPath}'");
        
        var preview = new SyncPreview();
        var sourceFiles = await GetFileListAsync(options.SourcePath, options.IncludeSubdirectories, cancellationToken);

        foreach (var sourceFile in sourceFiles)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            if (!_fileFilter.IsMatch(sourceFile.RelativePath, sourceFile.IsDirectory))
                continue;

            var targetPath = Path.Combine(options.TargetPath, sourceFile.RelativePath);

            if (sourceFile.IsDirectory)
            {
                if (!Directory.Exists(targetPath))
                {
                    preview.ToCopy.Add(sourceFile);
                }
                continue;
            }

            var action = DetermineAction(sourceFile, targetPath, options);
            
            switch (action)
            {
                case SyncAction.Copy:
                    preview.ToCopy.Add(sourceFile);
                    preview.TotalBytes += sourceFile.Size;
                    break;
                case SyncAction.Move:
                    preview.ToMove.Add(sourceFile);
                    preview.TotalBytes += sourceFile.Size;
                    break;
                case SyncAction.Delete:
                    preview.ToDelete.Add(sourceFile);
                    break;
                case SyncAction.Skip:
                    preview.ToSkip.Add(sourceFile);
                    break;
            }
        }

        if (options.Mode == SyncMode.Mirror)
        {
            var targetFiles = await GetFileListAsync(options.TargetPath, options.IncludeSubdirectories, cancellationToken);
            var sourceRelativePaths = new HashSet<string>(sourceFiles.Select(f => f.RelativePath), StringComparer.OrdinalIgnoreCase);

            foreach (var targetFile in targetFiles)
            {
                if (!sourceRelativePaths.Contains(targetFile.RelativePath) && _fileFilter.IsMatch(targetFile.RelativePath, targetFile.IsDirectory))
                {
                    preview.ToDelete.Add(targetFile);
                }
            }
        }

        _logger.Info($"Preview complete: {preview.ToCopy.Count} to copy, {preview.ToMove.Count} to move, {preview.ToDelete.Count} to delete, {preview.ToSkip.Count} to skip");
        return preview;
    }

    public async Task<SyncResult> SyncAsync(SyncOptions options, CancellationToken cancellationToken = default)
    {
        var result = new SyncResult
        {
            StartTime = DateTime.UtcNow
        };

        _logger.Info($"Starting sync from '{options.SourcePath}' to '{options.TargetPath}'");
        _logger.Info($"Mode: {options.Mode}, DryRun: {options.DryRun}");

        if (!_configManager.Validate(options))
        {
            result.Success = false;
            result.Errors.Add("Invalid configuration");
            result.EndTime = DateTime.UtcNow;
            return result;
        }

        try
        {
            if (!Directory.Exists(options.TargetPath))
            {
                Directory.CreateDirectory(options.TargetPath);
                _logger.Info($"Created target directory: {options.TargetPath}");
            }

            var preview = await PreviewAsync(options, cancellationToken);
            long processedFiles = 0;
            long processedBytes = 0;

            foreach (var file in preview.ToCopy)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var targetPath = Path.Combine(options.TargetPath, file.RelativePath);
                var opResult = await ExecuteFileOperationAsync(file, targetPath, SyncAction.Copy, options, cancellationToken);
                result.Operations.Add(opResult);
                
                if (opResult.Success)
                {
                    result.FilesCopied++;
                    result.BytesTransferred += opResult.BytesTransferred;
                }
                else
                {
                    result.FilesFailed++;
                    result.Errors.Add(opResult.ErrorMessage ?? $"Failed to copy: {file.FullPath}");
                }

                processedFiles++;
                processedBytes += file.Size;
                OnProgress(file.FullPath, preview.TotalFiles, processedFiles, preview.TotalBytes, processedBytes, SyncAction.Copy);
            }

            foreach (var file in preview.ToMove)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var targetPath = Path.Combine(options.TargetPath, file.RelativePath);
                var opResult = await ExecuteFileOperationAsync(file, targetPath, SyncAction.Move, options, cancellationToken);
                result.Operations.Add(opResult);
                
                if (opResult.Success)
                {
                    result.FilesMoved++;
                    result.BytesTransferred += opResult.BytesTransferred;
                }
                else
                {
                    result.FilesFailed++;
                    result.Errors.Add(opResult.ErrorMessage ?? $"Failed to move: {file.FullPath}");
                }

                processedFiles++;
                processedBytes += file.Size;
                OnProgress(file.FullPath, preview.TotalFiles, processedFiles, preview.TotalBytes, processedBytes, SyncAction.Move);
            }

            foreach (var file in preview.ToDelete)
            {
                if (cancellationToken.IsCancellationRequested)
                    break;

                var targetPath = Path.Combine(options.TargetPath, file.RelativePath);
                var opResult = await ExecuteFileOperationAsync(file, targetPath, SyncAction.Delete, options, cancellationToken);
                result.Operations.Add(opResult);
                
                if (opResult.Success)
                {
                    result.FilesDeleted++;
                }
                else
                {
                    result.FilesFailed++;
                    result.Errors.Add(opResult.ErrorMessage ?? $"Failed to delete: {targetPath}");
                }

                processedFiles++;
                OnProgress(targetPath, preview.TotalFiles, processedFiles, preview.TotalBytes, processedBytes, SyncAction.Delete);
            }

            result.FilesSkipped = preview.ToSkip.Count;
            result.Success = result.FilesFailed == 0;
        }
        catch (Exception ex)
        {
            _logger.Error("Sync failed with exception", ex);
            result.Success = false;
            result.Errors.Add(ex.Message);
        }

        result.EndTime = DateTime.UtcNow;
        result.Duration = result.EndTime - result.StartTime;
        
        _logger.Info($"Sync completed: {result.FilesCopied} copied, {result.FilesMoved} moved, {result.FilesDeleted} deleted, {result.FilesSkipped} skipped, {result.FilesFailed} failed");
        _logger.Info($"Duration: {result.Duration.TotalSeconds:F2}s, Speed: {result.SpeedMBps:F2} MB/s");

        return result;
    }

    private async Task<List<FileItem>> GetFileListAsync(string path, bool includeSubdirectories, CancellationToken cancellationToken)
    {
        var files = new List<FileItem>();
        
        if (!Directory.Exists(path))
        {
            _logger.Warning($"Directory does not exist: {path}");
            return files;
        }

        try
        {
            var searchOption = includeSubdirectories ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
            
            await Task.Run(() =>
            {
                var directoryInfo = new DirectoryInfo(path);
                
                if (includeSubdirectories)
                {
                    foreach (var dir in directoryInfo.EnumerateDirectories("*", searchOption))
                    {
                        if (cancellationToken.IsCancellationRequested)
                            break;
                        
                        try
                        {
                            files.Add(FileItem.FromDirectoryInfo(dir, path));
                        }
                        catch (UnauthorizedAccessException)
                        {
                            _logger.Warning($"Access denied to directory: {dir.FullName}");
                        }
                    }
                }

                foreach (var file in directoryInfo.EnumerateFiles("*", searchOption))
                {
                    if (cancellationToken.IsCancellationRequested)
                        break;
                    
                    try
                    {
                        files.Add(FileItem.FromFileInfo(file, path));
                    }
                    catch (UnauthorizedAccessException)
                    {
                        _logger.Warning($"Access denied to file: {file.FullName}");
                    }
                }
            }, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to enumerate files in: {path}", ex);
        }

        return files;
    }

    private SyncAction DetermineAction(FileItem sourceFile, string targetPath, SyncOptions options)
    {
        if (sourceFile.IsDirectory)
        {
            return Directory.Exists(targetPath) ? SyncAction.Skip : SyncAction.Copy;
        }

        if (!File.Exists(targetPath))
        {
            return options.Mode == SyncMode.Move ? SyncAction.Move : SyncAction.Copy;
        }

        var targetInfo = new FileInfo(targetPath);
        var needsUpdate = options.CompareMethod switch
        {
            CompareMethod.SizeOnly => sourceFile.Size != targetInfo.Length,
            CompareMethod.TimestampOnly => sourceFile.LastWriteTime > targetInfo.LastWriteTimeUtc,
            CompareMethod.TimestampAndSize => sourceFile.Size != targetInfo.Length || sourceFile.LastWriteTime > targetInfo.LastWriteTimeUtc,
            CompareMethod.Hash => ComputeFileHash(sourceFile.FullPath) != ComputeFileHash(targetPath),
            _ => false
        };

        if (!needsUpdate)
        {
            return SyncAction.Skip;
        }

        return options.Mode switch
        {
            SyncMode.Move => SyncAction.Move,
            SyncMode.Diff or SyncMode.Sync or SyncMode.Mirror => SyncAction.Copy,
            _ => SyncAction.Copy
        };
    }

    private async Task<FileOperationResult> ExecuteFileOperationAsync(FileItem sourceFile, string targetPath, SyncAction action, SyncOptions options, CancellationToken cancellationToken)
    {
        var result = new FileOperationResult
        {
            SourcePath = sourceFile.FullPath,
            TargetPath = targetPath,
            Action = action
        };

        var sw = System.Diagnostics.Stopwatch.StartNew();

        try
        {
            if (options.DryRun)
            {
                _logger.Info($"[DRY RUN] {action}: {sourceFile.FullPath} -> {targetPath}");
                result.Success = true;
                return result;
            }

            switch (action)
            {
                case SyncAction.Copy:
                    if (sourceFile.IsDirectory)
                    {
                        if (!Directory.Exists(targetPath))
                        {
                            Directory.CreateDirectory(targetPath);
                            _logger.Debug($"Created directory: {targetPath}");
                        }
                        result.Success = true;
                    }
                    else
                    {
                        result.BytesTransferred = await CopyFileWithRetryAsync(sourceFile.FullPath, targetPath, options, cancellationToken);
                        result.Success = true;
                        _logger.Debug($"Copied: {sourceFile.RelativePath} ({FormatBytes(result.BytesTransferred)})");
                    }
                    break;

                case SyncAction.Move:
                    if (sourceFile.IsDirectory)
                    {
                        if (!Directory.Exists(targetPath))
                        {
                            Directory.Move(sourceFile.FullPath, targetPath);
                        }
                        result.Success = true;
                    }
                    else
                    {
                        result.BytesTransferred = sourceFile.Size;
                        if (File.Exists(targetPath))
                        {
                            File.Delete(targetPath);
                        }
                        File.Move(sourceFile.FullPath, targetPath);
                        result.Success = true;
                        _logger.Debug($"Moved: {sourceFile.RelativePath}");
                    }
                    break;

                case SyncAction.Delete:
                    if (sourceFile.IsDirectory)
                    {
                        if (Directory.Exists(targetPath))
                        {
                            Directory.Delete(targetPath, true);
                        }
                        result.Success = true;
                    }
                    else if (File.Exists(targetPath))
                    {
                        File.Delete(targetPath);
                        result.Success = true;
                        _logger.Debug($"Deleted: {targetPath}");
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            _logger.Error($"Failed to {action} '{sourceFile.FullPath}'", ex);
            result.Success = false;
            result.ErrorMessage = ex.Message;
        }

        sw.Stop();
        result.Duration = sw.Elapsed;
        return result;
    }

    private async Task<long> CopyFileWithRetryAsync(string sourcePath, string targetPath, SyncOptions options, CancellationToken cancellationToken)
    {
        var targetDir = Path.GetDirectoryName(targetPath);
        if (!string.IsNullOrEmpty(targetDir) && !Directory.Exists(targetDir))
        {
            Directory.CreateDirectory(targetDir);
        }

        for (int retry = 0; retry <= options.MaxRetries; retry++)
        {
            try
            {
                return await CopyFileAsync(sourcePath, targetPath, options, cancellationToken);
            }
            catch (IOException ex) when (retry < options.MaxRetries)
            {
                _logger.Warning($"Retry {retry + 1}/{options.MaxRetries} for {sourcePath}: {ex.Message}");
                await Task.Delay(options.RetryDelayMs, cancellationToken);
            }
        }

        throw new IOException($"Failed to copy file after {options.MaxRetries} retries: {sourcePath}");
    }

    private async Task<long> CopyFileAsync(string sourcePath, string targetPath, SyncOptions options, CancellationToken cancellationToken)
    {
        var fileInfo = new FileInfo(sourcePath);
        long totalBytes = 0;

        using (var sourceStream = new FileStream(sourcePath, FileMode.Open, FileAccess.Read, FileShare.Read, options.BufferSize, FileOptions.SequentialScan | FileOptions.Asynchronous))
        using (var targetStream = new FileStream(targetPath, FileMode.Create, FileAccess.Write, FileShare.None, options.BufferSize, FileOptions.Asynchronous))
        {
            var buffer = new byte[options.BufferSize];
            int bytesRead;

            while ((bytesRead = await sourceStream.ReadAsync(buffer, cancellationToken)) > 0)
            {
                await targetStream.WriteAsync(buffer.AsMemory(0, bytesRead), cancellationToken);
                totalBytes += bytesRead;
            }
        }

        if (options.PreserveTimestamps)
        {
            File.SetLastWriteTimeUtc(targetPath, fileInfo.LastWriteTimeUtc);
            File.SetCreationTimeUtc(targetPath, fileInfo.CreationTimeUtc);
        }

        if (options.PreserveAttributes)
        {
            File.SetAttributes(targetPath, fileInfo.Attributes);
        }

        return totalBytes;
    }

    private static string ComputeFileHash(string filePath)
    {
        using var sha256 = SHA256.Create();
        using var stream = File.OpenRead(filePath);
        var hash = sha256.ComputeHash(stream);
        return Convert.ToHexString(hash);
    }

    private void OnProgress(string currentFile, long totalFiles, long processedFiles, long totalBytes, long processedBytes, SyncAction action)
    {
        ProgressChanged?.Invoke(this, new SyncProgressEventArgs
        {
            CurrentFile = currentFile,
            TotalFiles = totalFiles,
            ProcessedFiles = processedFiles,
            TotalBytes = totalBytes,
            ProcessedBytes = processedBytes,
            Action = action
        });
    }

    private static string FormatBytes(long bytes)
    {
        string[] suffixes = { "B", "KB", "MB", "GB", "TB" };
        int i = 0;
        double size = bytes;
        while (size >= 1024 && i < suffixes.Length - 1)
        {
            size /= 1024;
            i++;
        }
        return $"{size:F2} {suffixes[i]}";
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
        }
    }
}
