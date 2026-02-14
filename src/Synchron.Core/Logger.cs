using System.Collections.Concurrent;
using System.Text;
using Synchron.Core.Interfaces;

namespace Synchron.Core;

public sealed class Logger : ILogger, IDisposable
{
    private readonly object _lock = new();
    private volatile LogLevel _logLevel = LogLevel.Info;
    private readonly string? _logFilePath;
    private readonly bool _consoleOutput;
    private readonly ConcurrentQueue<string> _logQueue = new();
    private readonly Task? _writeTask;
    private readonly CancellationTokenSource _cts = new();
    private bool _disposed;

    public Logger(LogLevel level = LogLevel.Info, string? logFilePath = null, bool consoleOutput = true)
    {
        _logLevel = level;
        _logFilePath = logFilePath;
        _consoleOutput = consoleOutput;
        
        if (!string.IsNullOrEmpty(_logFilePath))
        {
            var directory = Path.GetDirectoryName(_logFilePath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
            {
                Directory.CreateDirectory(directory);
            }
            _writeTask = Task.Run(ProcessLogQueue);
        }
    }

    public void SetLogLevel(LogLevel level) => _logLevel = level;

    public void Debug(string message) => WriteLog(LogLevel.Debug, message);
    public void Info(string message) => WriteLog(LogLevel.Info, message);
    public void Warning(string message) => WriteLog(LogLevel.Warning, message);
    public void Error(string message) => WriteLog(LogLevel.Error, message);
    
    public void Error(string message, Exception exception)
    {
        var sb = new StringBuilder();
        sb.AppendLine(message);
        sb.AppendLine($"Exception: {exception.GetType().Name}");
        sb.AppendLine($"Message: {exception.Message}");
        sb.AppendLine($"StackTrace: {exception.StackTrace}");
        WriteLog(LogLevel.Error, sb.ToString());
    }

    private void WriteLog(LogLevel level, string message)
    {
        if (level < _logLevel) return;

        var timestamp = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff");
        var levelStr = level.ToString().ToUpper().PadLeft(7);
        var logEntry = $"[{timestamp}] [{levelStr}] {message}";

        if (_consoleOutput)
        {
            WriteToConsole(level, logEntry);
        }

        if (!string.IsNullOrEmpty(_logFilePath))
        {
            _logQueue.Enqueue(logEntry);
        }
    }

    private static void WriteToConsole(LogLevel level, string message)
    {
        var originalColor = Console.ForegroundColor;
        Console.ForegroundColor = level switch
        {
            LogLevel.Debug => ConsoleColor.Gray,
            LogLevel.Info => ConsoleColor.White,
            LogLevel.Warning => ConsoleColor.Yellow,
            LogLevel.Error => ConsoleColor.Red,
            _ => ConsoleColor.White
        };
        Console.WriteLine(message);
        Console.ForegroundColor = originalColor;
    }

    private async Task ProcessLogQueue()
    {
        var sb = new StringBuilder();
        
        while (!_cts.Token.IsCancellationRequested)
        {
            try
            {
                while (_logQueue.TryDequeue(out var logEntry))
                {
                    sb.AppendLine(logEntry);
                }

                if (sb.Length > 0)
                {
                    await File.AppendAllTextAsync(_logFilePath!, sb.ToString());
                    sb.Clear();
                }

                await Task.Delay(100, _cts.Token);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (IOException ex)
            {
                Console.Error.WriteLine($"Logger: Failed to write to log file: {ex.Message}");
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Logger: Unexpected error: {ex.Message}");
            }
        }

        while (_logQueue.TryDequeue(out var remainingEntry))
        {
            sb.AppendLine(remainingEntry);
        }
        
        if (sb.Length > 0)
        {
            try
            {
                await File.AppendAllTextAsync(_logFilePath!, sb.ToString());
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"Logger: Failed to flush remaining logs: {ex.Message}");
            }
        }
    }

    public void Dispose()
    {
        lock (_lock)
        {
            if (_disposed) return;
            _disposed = true;
        }
        
        _cts.Cancel();
        _writeTask?.Wait(TimeSpan.FromSeconds(5));
        _cts.Dispose();
    }
}
