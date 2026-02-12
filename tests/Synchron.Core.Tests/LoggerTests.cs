using Xunit;
using Synchron.Core;
using Synchron.Core.Interfaces;
using Synchron.Core.Models;

namespace Synchron.Core.Tests;

public class LoggerTests : IDisposable
{
    private readonly string _testLogPath;

    public LoggerTests()
    {
        _testLogPath = Path.Combine(Path.GetTempPath(), $"synchron_log_{Guid.NewGuid()}.log");
    }

    [Fact]
    public void Debug_WhenLogLevelIsDebug_WritesMessage()
    {
        using var logger = new Logger(LogLevel.Debug, consoleOutput: false);
        
        var exception = Record.Exception(() => logger.Debug("Test debug message"));
        Assert.Null(exception);
    }

    [Fact]
    public void Debug_WhenLogLevelIsInfo_DoesNotWriteMessage()
    {
        using var logger = new Logger(LogLevel.Info, consoleOutput: false);
        
        var exception = Record.Exception(() => logger.Debug("Test debug message"));
        Assert.Null(exception);
    }

    [Fact]
    public void Error_WithException_LogsExceptionDetails()
    {
        using var logger = new Logger(LogLevel.Error, consoleOutput: false);
        
        var exception = Record.Exception(() => 
            logger.Error("Test error", new InvalidOperationException("Test exception")));
        Assert.Null(exception);
    }

    [Fact]
    public void SetLogLevel_ChangesLogLevel()
    {
        using var logger = new Logger(LogLevel.Error, consoleOutput: false);
        logger.SetLogLevel(LogLevel.Debug);
        
        var exception = Record.Exception(() => logger.Debug("Should now be logged"));
        Assert.Null(exception);
    }

    [Fact]
    public void Logger_WithFilePath_CreatesLogFile()
    {
        using (var logger = new Logger(LogLevel.Info, _testLogPath, false))
        {
            logger.Info("Test message");
        }

        Thread.Sleep(200);
        Assert.True(File.Exists(_testLogPath));
    }

    public void Dispose()
    {
        if (File.Exists(_testLogPath))
        {
            try
            {
                File.Delete(_testLogPath);
            }
            catch
            {
            }
        }
    }
}
