using System;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace StickerApp.Services
{
    public class BackupService : IDisposable
    {
        private readonly LoggerService _logger;
        private readonly string _stickersPath;
        private readonly string _backupDirectory;
        private readonly object _lock = new();
        private Timer? _backupTimer;

        public BackupService(LoggerService logger)
        {
            _logger = logger;
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _stickersPath = Path.Combine(baseDir, "stickers.json");
            _backupDirectory = Path.Combine(baseDir, "backups");
        }

        public void RunInitialBackup()
        {
            // Initial backup verification
            CreateBackup();
            StartTimer(60); // Default hourly timer, dynamically configurable if needed
        }

        public void StartTimer(int intervalMinutes)
        {
            lock (_lock)
            {
                _backupTimer?.Dispose();
                _backupTimer = new Timer(
                    _ => CreateBackup(),
                    null,
                    TimeSpan.FromMinutes(intervalMinutes),
                    TimeSpan.FromMinutes(intervalMinutes));
            }
        }

        public bool CreateBackup()
        {
            try
            {
                if (!File.Exists(_stickersPath))
                {
                    return false;
                }

                if (!Directory.Exists(_backupDirectory))
                {
                    Directory.CreateDirectory(_backupDirectory);
                }

                var timestamp = DateTime.Now.ToString("yyyy_MM_dd_HHmmss");
                var destPath = Path.Combine(_backupDirectory, $"stickers_{timestamp}.json");
                File.Copy(_stickersPath, destPath, true);
                
                _logger.LogInfo($"Backup created: {Path.GetFileName(destPath)}");
                
                CleanupOldBackups();
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to create stickers backup", ex);
                return false;
            }
        }

        private void CleanupOldBackups()
        {
            try
            {
                var files = Directory.GetFiles(_backupDirectory, "stickers_*.json")
                                     .Select(f => new FileInfo(f))
                                     .OrderBy(f => f.CreationTime)
                                     .ToList();

                if (files.Count > 50)
                {
                    var toDelete = files.Count - 50;
                    for (int i = 0; i < toDelete; i++)
                    {
                        files[i].Delete();
                        _logger.LogInfo($"Cleaned up old backup file: {files[i].Name}");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error during old backups cleanup", ex);
            }
        }

        public void Dispose()
        {
            _backupTimer?.Dispose();
            GC.SuppressFinalize(this);
        }
    }
}
