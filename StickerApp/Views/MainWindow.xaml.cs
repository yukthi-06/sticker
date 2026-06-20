using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using StickerApp.Services;
using StickerApp.ViewModels;

namespace StickerApp.Views
{
    public partial class MainWindow : Window
    {
        public MainViewModel ViewModel { get; }
        private readonly DesktopHostService _desktopHost;
        private readonly TrayService _tray;
        private readonly LoggerService _logger;
        private readonly Dictionary<Guid, StickerView> _activeStickerWindows = new();

        public MainWindow(MainViewModel viewModel, DesktopHostService desktopHost, TrayService tray, LoggerService logger)
        {
            this.InitializeComponent();
            ViewModel = viewModel;
            _desktopHost = desktopHost;
            _tray = tray;
            _logger = logger;

            // Hide the orchestrator window from the taskbar and screen
            var appWindow = this.AppWindow;
            appWindow.IsShownInSwitchers = false; // Hide from Alt+Tab
            appWindow.Hide();

            // Setup listeners
            ViewModel.StickerWindowRequested += OnStickerWindowRequested;
            ViewModel.SettingsWindowRequested += OnSettingsWindowRequested;

            _tray.NewStickerRequested += (s, e) => ViewModel.CreateNewSticker();
            _tray.ShowAllRequested += (s, e) => ViewModel.ShowAllStickers();
            _tray.SettingsRequested += (s, e) => OnSettingsWindowRequested();
            _tray.RestoreDeletedRequested += (s, e) => OnSettingsWindowRequested(); // Settings window contains Recycle Bin
            _tray.ExitRequested += (s, e) => ExitApplication();
        }

        private void OnStickerWindowRequested(StickerViewModel vm)
        {
            if (_activeStickerWindows.TryGetValue(vm.Id, out var existingWindow))
            {
                existingWindow.Activate();
                return;
            }

            // Create a new StickerView for this sticker
            var stickerView = new StickerView(vm, _desktopHost, App.Services.GetRequiredService<AnimationService>());
            _activeStickerWindows[vm.Id] = stickerView;
            
            // Listen to close/deleted requests
            vm.CloseRequested += () =>
            {
                if (_activeStickerWindows.Remove(vm.Id))
                {
                    stickerView.Close();
                }
            };

            stickerView.Activate();
        }

        private void OnSettingsWindowRequested()
        {
            var settingsView = App.Services.GetRequiredService<SettingsView>();
            settingsView.Activate();
        }

        private void ExitApplication()
        {
            _logger.LogInfo("Application shutting down via Tray Menu.");
            
            // Remove tray icon
            _tray.Dispose();

            // Close all sticker windows
            foreach (var window in _activeStickerWindows.Values)
            {
                window.Close();
            }

            Application.Current.Exit();
        }
    }
}
