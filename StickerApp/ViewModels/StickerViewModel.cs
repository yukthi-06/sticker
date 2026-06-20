using System;
using System.Threading.Tasks;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using StickerApp.Models;
using StickerApp.Services;

namespace StickerApp.ViewModels
{
    public partial class StickerViewModel : ObservableObject
    {
        private readonly JsonStorageService _storage;
        private readonly LoggerService _logger;
        private readonly MainViewModel _mainViewModel;

        public StickerModel Model { get; }

        public StickerViewModel(StickerModel model, JsonStorageService storage, LoggerService logger, MainViewModel mainViewModel)
        {
            Model = model;
            _storage = storage;
            _logger = logger;
            _mainViewModel = mainViewModel;
        }

        public Guid Id => Model.Id;

        public string Title
        {
            get => Model.Title;
            set
            {
                if (Model.Title != value)
                {
                    Model.Title = value;
                    OnPropertyChanged();
                    SaveStickerState();
                }
            }
        }

        public string Text
        {
            get => Model.Text;
            set
            {
                if (Model.Text != value)
                {
                    Model.Text = value;
                    OnPropertyChanged();
                    SaveStickerState();
                }
            }
        }

        public double X
        {
            get => Model.X;
            set
            {
                if (Math.Abs(Model.X - value) > 0.1)
                {
                    Model.X = value;
                    OnPropertyChanged();
                    SaveStickerState();
                }
            }
        }

        public double Y
        {
            get => Model.Y;
            set
            {
                if (Math.Abs(Model.Y - value) > 0.1)
                {
                    Model.Y = value;
                    OnPropertyChanged();
                    SaveStickerState();
                }
            }
        }

        public double Width
        {
            get => Model.Width;
            set
            {
                if (Math.Abs(Model.Width - value) > 0.1)
                {
                    Model.Width = value;
                    OnPropertyChanged();
                    SaveStickerState();
                }
            }
        }

        public double Height
        {
            get => Model.Height;
            set
            {
                if (Math.Abs(Model.Height - value) > 0.1)
                {
                    Model.Height = value;
                    OnPropertyChanged();
                    SaveStickerState();
                }
            }
        }

        public double Opacity
        {
            get => Model.Opacity;
            set
            {
                if (Math.Abs(Model.Opacity - value) > 0.01)
                {
                    Model.Opacity = value;
                    OnPropertyChanged();
                    SaveStickerState();
                }
            }
        }

        public string Color
        {
            get => Model.Color;
            set
            {
                if (Model.Color != value)
                {
                    Model.Color = value;
                    OnPropertyChanged();
                    SaveStickerState();
                }
            }
        }

        public bool Locked
        {
            get => Model.Locked;
            set
            {
                if (Model.Locked != value)
                {
                    Model.Locked = value;
                    OnPropertyChanged();
                    SaveStickerState();
                    LockedChanged?.Invoke(this, value);
                }
            }
        }

        public string Font
        {
            get => Model.Font;
            set
            {
                if (Model.Font != value)
                {
                    Model.Font = value;
                    OnPropertyChanged();
                    SaveStickerState();
                }
            }
        }

        public double FontSize
        {
            get => Model.FontSize;
            set
            {
                if (Math.Abs(Model.FontSize - value) > 0.1)
                {
                    Model.FontSize = value;
                    OnPropertyChanged();
                    SaveStickerState();
                }
            }
        }

        public string FontColor
        {
            get => Model.FontColor;
            set
            {
                if (Model.FontColor != value)
                {
                    Model.FontColor = value;
                    OnPropertyChanged();
                    SaveStickerState();
                }
            }
        }

        public bool IsAlwaysOnTop
        {
            get => Model.IsAlwaysOnTop;
            set
            {
                if (Model.IsAlwaysOnTop != value)
                {
                    Model.IsAlwaysOnTop = value;
                    OnPropertyChanged();
                    SaveStickerState();
                    AlwaysOnTopChanged?.Invoke(this, value);
                }
            }
        }

        public bool IsClickThrough
        {
            get => Model.IsClickThrough;
            set
            {
                if (Model.IsClickThrough != value)
                {
                    Model.IsClickThrough = value;
                    OnPropertyChanged();
                    SaveStickerState();
                    ClickThroughChanged?.Invoke(this, value);
                }
            }
        }

        public event EventHandler<bool>? LockedChanged;
        public event EventHandler<bool>? AlwaysOnTopChanged;
        public event EventHandler<bool>? ClickThroughChanged;
        public event Action? CloseRequested;

        [RelayCommand]
        public void ToggleLock() => Locked = !Locked;

        [RelayCommand]
        public void ToggleAlwaysOnTop() => IsAlwaysOnTop = !IsAlwaysOnTop;

        [RelayCommand]
        public void ToggleClickThrough() => IsClickThrough = !IsClickThrough;

        [RelayCommand]
        public async Task DeleteSticker()
        {
            CloseRequested?.Invoke();
            await _mainViewModel.RemoveStickerAsync(this);
        }

        [RelayCommand]
        public async Task DuplicateSticker()
        {
            await _mainViewModel.DuplicateStickerAsync(this);
        }

        private void SaveStickerState()
        {
            Model.UpdatedAt = DateTime.UtcNow;
            _ = _storage.SaveStickersAsync(_storage.Stickers);
        }
    }
}
