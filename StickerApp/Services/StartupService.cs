using System;
using Microsoft.Win32;

namespace StickerApp.Services
{
    public class StartupService
    {
        private readonly LoggerService _logger;
        private const string RunKeyPath = @"Software\Microsoft\Windows\CurrentVersion\Run";
        private const string AppName = "StickerApp";

        public StartupService(LoggerService logger)
        {
            _logger = logger;
        }

        public bool IsStartupEnabled()
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, false);
                if (key == null) return false;
                var value = key.GetValue(AppName);
                return value != null;
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to read startup registry key", ex);
                return false;
            }
        }

        public void SetStartup(bool enable)
        {
            try
            {
                using var key = Registry.CurrentUser.OpenSubKey(RunKeyPath, true);
                if (key == null)
                {
                    _logger.LogWarning("Startup registry path not found");
                    return;
                }

                if (enable)
                {
                    // Get current executable path
                    var processPath = Environment.ProcessPath;
                    if (string.IsNullOrEmpty(processPath))
                    {
                        processPath = AppDomain.CurrentDomain.BaseDirectory + "StickerApp.exe";
                    }

                    // Format path with double quotes to handle spaces
                    key.SetValue(AppName, $"\"{processPath}\"");
                    _logger.LogInfo($"Enabled startup with Windows: {processPath}");
                }
                else
                {
                    key.DeleteValue(AppName, false);
                    _logger.LogInfo("Disabled startup with Windows");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to update startup registry key", ex);
            }
        }
    }
}
