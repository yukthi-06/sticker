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
    public partial class MainViewModel : ObservableObject
    {
        private readonly JsonStorageService _storage;
        private readonly LoggerService _logger;

        [ObservableProperty]
        private ObservableCollection<StickerViewModel> _stickers = new();

        public event Action<StickerViewModel>? StickerWindowRequested;
        public event Action? SettingsWindowRequested;

        public MainViewModel(JsonStorageService storage, LoggerService logger)
        {
            _storage = storage;
            _logger = logger;
            
            LoadStickers();
            _storage.StickersChanged += (s, e) => LoadStickers();
        }

        private void LoadStickers()
        {
            var activeStickers = _storage.Stickers
                .Where(s => !s.Deleted)
                .Select(s => new StickerViewModel(s, _storage, _logger, this))
                .ToList();

            Stickers = new ObservableCollection<StickerViewModel>(activeStickers);

            // Notify MainWindow to spawn views for loaded stickers
            foreach (var vm in Stickers)
            {
                StickerWindowRequested?.Invoke(vm);
            }
        }

        [RelayCommand]
        public void CreateNewSticker()
        {
            var newModel = new StickerModel
            {
                Id = Guid.NewGuid(),
                Title = "New Sticker",
                Text = "Type something...",
                X = 150,
                Y = 150,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _storage.Stickers.Add(newModel);
            var vm = new StickerViewModel(newModel, _storage, _logger, this);
            Stickers.Add(vm);
            
            // Save state
            _ = _storage.SaveStickersAsync(_storage.Stickers);

            StickerWindowRequested?.Invoke(vm);
            _logger.LogInfo($"Created new sticker: {newModel.Id}");
        }

        [RelayCommand]
        public void ShowAllStickers()
        {
            foreach (var vm in Stickers)
            {
                StickerWindowRequested?.Invoke(vm);
            }
        }

        [RelayCommand]
        public void ShowSettings()
        {
            SettingsWindowRequested?.Invoke();
        }

        public async Task RemoveStickerAsync(StickerViewModel vm)
        {
            Stickers.Remove(vm);
            
            // Soft delete
            var model = _storage.Stickers.FirstOrDefault(s => s.Id == vm.Model.Id);
            if (model != null)
            {
                model.Deleted = true;
                model.DeletedAt = DateTime.UtcNow;
            }

            await _storage.SaveStickersAsync(_storage.Stickers);
            _logger.LogInfo($"Soft-deleted sticker: {vm.Model.Id}");
        }

        public async Task DuplicateStickerAsync(StickerViewModel vm)
        {
            var newModel = new StickerModel
            {
                Id = Guid.NewGuid(),
                Title = $"{vm.Model.Title} (Copy)",
                Text = vm.Model.Text,
                X = vm.Model.X + 25,
                Y = vm.Model.Y + 25,
                Width = vm.Model.Width,
                Height = vm.Model.Height,
                Color = vm.Model.Color,
                Font = vm.Model.Font,
                FontSize = vm.Model.FontSize,
                FontColor = vm.Model.FontColor,
                Opacity = vm.Model.Opacity,
                IsAlwaysOnTop = vm.Model.IsAlwaysOnTop,
                IsClickThrough = vm.Model.IsClickThrough,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _storage.Stickers.Add(newModel);
            var newVm = new StickerViewModel(newModel, _storage, _logger, this);
            Stickers.Add(newVm);

            await _storage.SaveStickersAsync(_storage.Stickers);
            StickerWindowRequested?.Invoke(newVm);
            _logger.LogInfo($"Duplicated sticker: {vm.Model.Id} -> {newModel.Id}");
        }
    }
}
