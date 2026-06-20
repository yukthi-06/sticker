using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI.Xaml;
using System;
using StickerApp.Services;
using StickerApp.ViewModels;
using StickerApp.Views;

namespace StickerApp
{
    public partial class App : Application
    {
        public static IServiceProvider Services { get; private set; } = null!;
        public static Window? MainWindow { get; private set; }

        public App()
        {
            this.InitializeComponent();
            ConfigureServices();
        }

        private void ConfigureServices()
        {
            var services = new ServiceCollection();

            // Core persistence & diagnostic services
            services.AddSingleton<LoggerService>();
            services.AddSingleton<JsonStorageService>();
            services.AddSingleton<BackupService>();
            services.AddSingleton<DesktopHostService>();
            services.AddSingleton<NotificationService>();
            services.AddSingleton<TrayService>();
            services.AddSingleton<StartupService>();
            services.AddSingleton<AnimationService>();

            // ViewModels
            services.AddSingleton<MainViewModel>();
            services.AddSingleton<SettingsViewModel>();

            // Views (we resolve views when needed)
            services.AddTransient<MainWindow>();
            services.AddTransient<SettingsView>();

            Services = services.BuildServiceProvider();
        }

        protected override void OnLaunched(LaunchActivatedEventArgs args)
        {
            // Initialize system tray and run basic startup/backup services
            var logger = Services.GetRequiredService<LoggerService>();
            logger.LogInfo("Application launched.");

            // Start storage and load data
            var storage = Services.GetRequiredService<JsonStorageService>();
            storage.Initialize();

            // Run initial backup checks
            var backup = Services.GetRequiredService<BackupService>();
            backup.RunInitialBackup();

            // Setup tray service
            var tray = Services.GetRequiredService<TrayService>();
            tray.Initialize();

            // Startup notification
            var notifications = Services.GetRequiredService<NotificationService>();
            notifications.ShowToast("Sticker App", "Desktop Sticker application is running in the background.");

            // Open orchestration MainWindow
            MainWindow = Services.GetRequiredService<MainWindow>();
            MainWindow.Activate();
        }
    }
}
