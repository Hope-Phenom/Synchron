namespace Synchron.Core.Interfaces;

public interface ILogger
{
    void Debug(string message);
    void Info(string message);
    void Warning(string message);
    void Error(string message);
    void Error(string message, Exception exception);
    void SetLogLevel(LogLevel level);
}

public enum LogLevel
{
    Debug = 0,
    Info = 1,
    Warning = 2,
    Error = 3,
    None = 4
}
