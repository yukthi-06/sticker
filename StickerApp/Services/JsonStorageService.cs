using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using StickerApp.Models;

namespace StickerApp.Services
{
    public class JsonStorageService
    {
        private readonly LoggerService _logger;
        private readonly string _settingsPath;
        private readonly string _stickersPath;
        private readonly SemaphoreSlim _settingsLock = new(1, 1);
        private readonly SemaphoreSlim _stickersLock = new(1, 1);

        public SettingsModel Settings { get; private set; } = new();
        public List<StickerModel> Stickers { get; private set; } = new();

        public event EventHandler? StickersChanged;
        public event EventHandler? SettingsChanged;

        public JsonStorageService(LoggerService logger)
        {
            _logger = logger;
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            _settingsPath = Path.Combine(baseDir, "settings.json");
            _stickersPath = Path.Combine(baseDir, "stickers.json");
        }

        public void Initialize()
        {
            LoadSettings();
            LoadStickers();
        }

        public void LoadSettings()
        {
            _settingsLock.Wait();
            try
            {
                if (!File.Exists(_settingsPath))
                {
                    Settings = new SettingsModel();
                    SaveSettingsSyncInternal();
                    _logger.LogInfo("Created default settings.json");
                    return;
                }

                var json = File.ReadAllText(_settingsPath);
                var loaded = JsonSerializer.Deserialize<SettingsModel>(json);
                if (loaded != null)
                {
                    Settings = loaded;
                }
                else
                {
                    throw new JsonException("Deserialized settings was null.");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error loading settings.json, resetting to default", ex);
                Settings = new SettingsModel();
                SaveSettingsSyncInternal();
            }
            finally
            {
                _settingsLock.Release();
            }
        }

        public void LoadStickers()
        {
            _stickersLock.Wait();
            try
            {
                if (!File.Exists(_stickersPath))
                {
                    Stickers = new List<StickerModel>();
                    SaveStickersSyncInternal();
                    _logger.LogInfo("Created empty stickers.json");
                    return;
                }

                var json = File.ReadAllText(_stickersPath);
                var loaded = JsonSerializer.Deserialize<Dictionary<string, List<StickerModel>>>(json);
                if (loaded != null && loaded.TryGetValue("stickers", out var list))
                {
                    Stickers = list;
                }
                else
                {
                    // Fallback to try parsing directly as a list
                    var directList = JsonSerializer.Deserialize<List<StickerModel>>(json);
                    if (directList != null)
                    {
                        Stickers = directList;
                    }
                    else
                    {
                        throw new JsonException("Stickers list format invalid.");
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError("Error loading stickers.json, attempting backup recovery", ex);
                RecoverFromBackup();
            }
            finally
            {
                _stickersLock.Release();
            }
        }

        private void RecoverFromBackup()
        {
            try
            {
                var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                var backupDir = Path.Combine(baseDir, "backups");
                if (Directory.Exists(backupDir))
                {
                    var files = Directory.GetFiles(backupDir, "stickers_*.json");
                    if (files.Length > 0)
                    {
                        Array.Sort(files);
                        var newestBackup = files[^1];
                        _logger.LogInfo($"Recovering stickers from backup: {newestBackup}");
                        var json = File.ReadAllText(newestBackup);
                        var loaded = JsonSerializer.Deserialize<Dictionary<string, List<StickerModel>>>(json);
                        if (loaded != null && loaded.TryGetValue("stickers", out var list))
                        {
                            Stickers = list;
                            SaveStickersSyncInternal();
                            return;
                        }
                    }
                }
            }
            catch (Exception backupEx)
            {
                _logger.LogError("Failed to recover stickers from backup", backupEx);
            }

            // Absolute fallback
            Stickers = new List<StickerModel>();
            SaveStickersSyncInternal();
        }

        public async Task SaveSettingsAsync(SettingsModel settings)
        {
            await _settingsLock.WaitAsync();
            try
            {
                Settings = settings;
                var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_settingsPath, json);
                SettingsChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to save settings.json asynchronously", ex);
            }
            finally
            {
                _settingsLock.Release();
            }
        }

        public async Task SaveStickersAsync(List<StickerModel> stickers)
        {
            await _stickersLock.WaitAsync();
            try
            {
                Stickers = stickers;
                var wrapper = new Dictionary<string, List<StickerModel>> { { "stickers", Stickers } };
                var json = JsonSerializer.Serialize(wrapper, new JsonSerializerOptions { WriteIndented = true });
                await File.WriteAllTextAsync(_stickersPath, json);
                StickersChanged?.Invoke(this, EventArgs.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to save stickers.json asynchronously", ex);
            }
            finally
            {
                _stickersLock.Release();
            }
        }

        private void SaveSettingsSyncInternal()
        {
            try
            {
                var json = JsonSerializer.Serialize(Settings, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_settingsPath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to save settings.json synchronously", ex);
            }
        }

        private void SaveStickersSyncInternal()
        {
            try
            {
                var wrapper = new Dictionary<string, List<StickerModel>> { { "stickers", Stickers } };
                var json = JsonSerializer.Serialize(wrapper, new JsonSerializerOptions { WriteIndented = true });
                File.WriteAllText(_stickersPath, json);
            }
            catch (Exception ex)
            {
                _logger.LogError("Failed to save stickers.json synchronously", ex);
            }
        }
    }
}
