using System;
using System.Collections.ObjectModel;
using System.Linq;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StickerApp.Models;
using StickerApp.Services;

namespace StickerApp.ViewModels
{
    public partial class SettingsViewModel : ObservableObject
    {
        private readonly JsonStorageService _storage;
        private readonly StartupService _startupService;
        private readonly BackupService _backupService;
        private readonly LoggerService _logger;
        private readonly MainViewModel _mainViewModel;

        [ObservableProperty]
        private string _theme;

        [ObservableProperty]
        private bool _startWithWindows;

        [ObservableProperty]
        private bool _trayEnabled;

        [ObservableProperty]
        private bool _animationsEnabled;

        [ObservableProperty]
        private ObservableCollection<StickerModel> _deletedStickers = new();

        public SettingsViewModel(
            JsonStorageService storage,
            StartupService startupService,
            BackupService backupService,
            LoggerService logger,
            MainViewModel mainViewModel)
        {
            _storage = storage;
            _startupService = startupService;
            _backupService = backupService;
            _logger = logger;
            _mainViewModel = mainViewModel;

            _theme = _storage.Settings.Theme;
            _startWithWindows = _storage.Settings.StartWithWindows;
            _trayEnabled = _storage.Settings.TrayEnabled;
            _animationsEnabled = _storage.Settings.AnimationsEnabled;

            LoadDeletedStickers();
        }

        private void LoadDeletedStickers()
        {
            var deleted = _storage.Stickers.Where(s => s.Deleted).ToList();
            DeletedStickers = new ObservableCollection<StickerModel>(deleted);
        }

        [RelayCommand]
        public async Task SaveSettingsAsync()
        {
            var settings = _storage.Settings;
            settings.Theme = Theme;
            settings.StartWithWindows = StartWithWindows;
            settings.TrayEnabled = TrayEnabled;
            settings.AnimationsEnabled = AnimationsEnabled;

            // Apply system startup config
            _startupService.SetStartup(StartWithWindows);

            await _storage.SaveSettingsAsync(settings);
            _logger.LogInfo("Application settings updated.");
        }

        [RelayCommand]
        public void ManualBackup()
        {
            bool success = _backupService.CreateBackup();
            if (success)
            {
                _logger.LogInfo("Manual backup triggered successfully.");
            }
        }

        [RelayCommand]
        public async Task RestoreStickerAsync(StickerModel model)
        {
            model.Deleted = false;
            model.DeletedAt = null;
            DeletedStickers.Remove(model);

            // Re-load main view stickers
            await _storage.SaveStickersAsync(_storage.Stickers);
            _logger.LogInfo($"Restored sticker from Recycle Bin: {model.Id}");
        }

        [RelayCommand]
        public async Task PermanentlyDeleteStickerAsync(StickerModel model)
        {
            _storage.Stickers.Remove(model);
            DeletedStickers.Remove(model);

            await _storage.SaveStickersAsync(_storage.Stickers);
            _logger.LogInfo($"Permanently deleted sticker: {model.Id}");
        }
    }
}
