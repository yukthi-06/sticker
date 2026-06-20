using System;
using System.IO;

namespace StickerApp.Services
{
    public class LoggerService
    {
        private readonly string _logDirectory;
        private readonly string _logFilePath;
        private readonly object _lock = new();

        public LoggerService()
        {
            // All paths are relative to the executable for portability
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _logDirectory = Path.Combine(baseDir, "logs");
            _logFilePath = Path.Combine(_logDirectory, "app.log");
        }

        public void LogInfo(string message) => WriteLog("INFO", message);
        public void LogWarning(string message) => WriteLog("WARN", message);
        public void LogError(string message, Exception? ex = null)
        {
            var fullMessage = ex != null ? $"{message} | Exception: {ex.Message}\nStack: {ex.StackTrace}" : message;
            WriteLog("ERROR", fullMessage);
        }

        private void WriteLog(string level, string message)
        {
            try
            {
                lock (_lock)
                {
                    if (!Directory.Exists(_logDirectory))
                    {
                        Directory.CreateDirectory(_logDirectory);
                    }

                    var logLine = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}] {message}";
                    
                    // Simple rotation: if log file exceeds 5MB, archive it
                    if (File.Exists(_logFilePath) && new FileInfo(_logFilePath).Length > 5 * 1024 * 1024)
                    {
                        var archivePath = Path.Combine(_logDirectory, $"app_{DateTime.Now:yyyyMMdd_HHmmss}.log");
                        File.Move(_logFilePath, archivePath);
                    }

                    using var writer = new StreamWriter(_logFilePath, true);
                    writer.WriteLine(logLine);
                }
            }
            catch
            {
                // Portable app rule: Fail silently on disk lock, never crash
            }
        }
    }
}
