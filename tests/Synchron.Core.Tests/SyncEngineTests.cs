using Xunit;
using Synchron.Core;
using Synchron.Core.Interfaces;
using Synchron.Core.Models;

namespace Synchron.Core.Tests;

public class SyncEngineTests : IDisposable
{
    private readonly ILogger _logger;
    private readonly FileFilter _fileFilter;
    private readonly ConfigManager _configManager;
    private readonly SyncEngine _syncEngine;
    private readonly string _sourceDir;
    private readonly string _targetDir;

    public SyncEngineTests()
    {
        _logger = new Logger(LogLevel.None);
        _fileFilter = new FileFilter(_logger);
        _configManager = new ConfigManager(_logger);
        _syncEngine = new SyncEngine(_logger, _fileFilter, _configManager);

        var tempBase = Path.GetTempPath();
        _sourceDir = Path.Combine(tempBase, $"synchron_source_{Guid.NewGuid()}");
        _targetDir = Path.Combine(tempBase, $"synchron_target_{Guid.NewGuid()}");
        
        Directory.CreateDirectory(_sourceDir);
        Directory.CreateDirectory(_targetDir);
    }

    [Fact]
    public async Task PreviewAsync_EmptySource_ReturnsEmptyPreview()
    {
        var options = new SyncOptions
        {
            SourcePath = _sourceDir,
            TargetPath = _targetDir
        };

        var preview = await _syncEngine.PreviewAsync(options);

        Assert.Empty(preview.ToCopy);
        Assert.Empty(preview.ToMove);
        Assert.Empty(preview.ToDelete);
        Assert.Empty(preview.ToSkip);
    }

    [Fact]
    public async Task PreviewAsync_NewFile_ReturnsCopyAction()
    {
        var testFile = Path.Combine(_sourceDir, "test.txt");
        await File.WriteAllTextAsync(testFile, "test content");

        var options = new SyncOptions
        {
            SourcePath = _sourceDir,
            TargetPath = _targetDir
        };

        var preview = await _syncEngine.PreviewAsync(options);

        Assert.Single(preview.ToCopy);
        Assert.Equal(12, preview.TotalBytes);
    }

    [Fact]
    public async Task SyncAsync_NewFile_CopiesFile()
    {
        var testFile = Path.Combine(_sourceDir, "test.txt");
        await File.WriteAllTextAsync(testFile, "test content");

        var options = new SyncOptions
        {
            SourcePath = _sourceDir,
            TargetPath = _targetDir
        };

        var result = await _syncEngine.SyncAsync(options);

        Assert.True(result.Success);
        Assert.Equal(1, result.FilesCopied);
        Assert.True(File.Exists(Path.Combine(_targetDir, "test.txt")));
    }

    [Fact]
    public async Task SyncAsync_DryRun_DoesNotCopyFile()
    {
        var testFile = Path.Combine(_sourceDir, "test.txt");
        await File.WriteAllTextAsync(testFile, "test content");

        var options = new SyncOptions
        {
            SourcePath = _sourceDir,
            TargetPath = _targetDir,
            DryRun = true
        };

        var result = await _syncEngine.SyncAsync(options);

        Assert.True(result.Success);
        Assert.Equal(1, result.FilesCopied);
        Assert.False(File.Exists(Path.Combine(_targetDir, "test.txt")));
    }

    [Fact]
    public async Task SyncAsync_SameFile_SkipsCopy()
    {
        var sourceFile = Path.Combine(_sourceDir, "test.txt");
        var targetFile = Path.Combine(_targetDir, "test.txt");
        
        await File.WriteAllTextAsync(sourceFile, "same content");
        await File.WriteAllTextAsync(targetFile, "same content");
        File.SetLastWriteTimeUtc(targetFile, DateTime.UtcNow);

        var options = new SyncOptions
        {
            SourcePath = _sourceDir,
            TargetPath = _targetDir
        };

        var result = await _syncEngine.SyncAsync(options);

        Assert.True(result.Success);
        Assert.Equal(0, result.FilesCopied);
        Assert.Equal(1, result.FilesSkipped);
    }

    [Fact]
    public async Task SyncAsync_WithSubdirectories_CopiesRecursively()
    {
        var subDir = Path.Combine(_sourceDir, "subfolder");
        Directory.CreateDirectory(subDir);
        await File.WriteAllTextAsync(Path.Combine(subDir, "nested.txt"), "nested content");

        var options = new SyncOptions
        {
            SourcePath = _sourceDir,
            TargetPath = _targetDir,
            IncludeSubdirectories = true
        };

        var result = await _syncEngine.SyncAsync(options);

        Assert.True(result.Success);
        Assert.True(File.Exists(Path.Combine(_targetDir, "subfolder", "nested.txt")));
    }

    [Fact]
    public async Task SyncAsync_MoveMode_MovesFile()
    {
        var testFile = Path.Combine(_sourceDir, "test.txt");
        await File.WriteAllTextAsync(testFile, "test content");

        var options = new SyncOptions
        {
            SourcePath = _sourceDir,
            TargetPath = _targetDir,
            Mode = SyncMode.Move
        };

        var result = await _syncEngine.SyncAsync(options);

        Assert.True(result.Success);
        Assert.Equal(1, result.FilesMoved);
        Assert.False(File.Exists(testFile));
        Assert.True(File.Exists(Path.Combine(_targetDir, "test.txt")));
    }

    [Fact]
    public async Task SyncAsync_MirrorMode_DeletesExtraFiles()
    {
        var sourceFile = Path.Combine(_sourceDir, "source.txt");
        var targetOnlyFile = Path.Combine(_targetDir, "target_only.txt");
        
        await File.WriteAllTextAsync(sourceFile, "source content");
        await File.WriteAllTextAsync(targetOnlyFile, "target only content");

        var options = new SyncOptions
        {
            SourcePath = _sourceDir,
            TargetPath = _targetDir,
            Mode = SyncMode.Mirror
        };

        var result = await _syncEngine.SyncAsync(options);

        Assert.True(result.Success);
        Assert.Equal(1, result.FilesCopied);
        Assert.Equal(1, result.FilesDeleted);
        Assert.False(File.Exists(targetOnlyFile));
    }

    [Fact]
    public async Task SyncAsync_WithFilter_OnlyMatchesFilteredFiles()
    {
        await File.WriteAllTextAsync(Path.Combine(_sourceDir, "test.txt"), "txt content");
        await File.WriteAllTextAsync(Path.Combine(_sourceDir, "test.cs"), "cs content");

        _fileFilter.AddIncludePattern("*.txt");
        
        var options = new SyncOptions
        {
            SourcePath = _sourceDir,
            TargetPath = _targetDir
        };

        var result = await _syncEngine.SyncAsync(options);

        Assert.True(result.Success);
        Assert.Equal(1, result.FilesCopied);
        Assert.True(File.Exists(Path.Combine(_targetDir, "test.txt")));
        Assert.False(File.Exists(Path.Combine(_targetDir, "test.cs")));
    }

    public void Dispose()
    {
        if (Directory.Exists(_sourceDir))
        {
            Directory.Delete(_sourceDir, true);
        }
        if (Directory.Exists(_targetDir))
        {
            Directory.Delete(_targetDir, true);
        }
        _syncEngine.Dispose();
    }
}
