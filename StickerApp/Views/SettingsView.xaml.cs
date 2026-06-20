using System;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StickerApp.Models;
using StickerApp.ViewModels;

namespace StickerApp.Views
{
    public partial class SettingsView : Window
    {
        public SettingsViewModel ViewModel { get; }
        private bool _isLoading = true;

        public SettingsView(SettingsViewModel viewModel)
        {
            this.InitializeComponent();
            ViewModel = viewModel;
            
            // Set AppWindow configuration
            var appWindow = this.AppWindow;
            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            appWindow.IsShownInSwitchers = true; // Show in Alt+Tab and taskbar

            // Bind Navigation Events
            SettingsNav.SelectionChanged += SettingsNav_SelectionChanged;
            
            // Initialize fields
            InitializeFormValues();
            _isLoading = false;
        }

        private void InitializeFormValues()
        {
            // Set active navigation selection
            SettingsNav.SelectedItem = SettingsNav.MenuItems[0];

            // Theme combobox selection
            foreach (ComboBoxItem item in ThemeComboBox.Items)
            {
                if (item.Tag?.ToString() == ViewModel.Theme)
                {
                    ThemeComboBox.SelectedItem = item;
                    break;
                }
            }

            // Toggles
            StartupToggle.IsOn = ViewModel.StartWithWindows;
            TrayToggle.IsOn = ViewModel.TrayEnabled;
            AnimationsToggle.IsOn = ViewModel.AnimationsEnabled;

            // Soft-deleted list
            DeletedStickersList.ItemsSource = ViewModel.DeletedStickers;
        }

        private void SettingsNav_SelectionChanged(NavigationView sender, NavigationViewSelectionChangedEventArgs args)
        {
            if (args.SelectedItemContainer is NavigationViewItem item)
            {
                var tag = item.Tag?.ToString();
                if (tag == "general")
                {
                    GeneralPanel.Visibility = Visibility.Visible;
                    RecyclePanel.Visibility = Visibility.Collapsed;
                }
                else if (tag == "recycle")
                {
                    GeneralPanel.Visibility = Visibility.Collapsed;
                    RecyclePanel.Visibility = Visibility.Visible;
                }
            }
        }

        private async void ThemeComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (_isLoading) return;
            if (ThemeComboBox.SelectedItem is ComboBoxItem item && item.Tag != null)
            {
                ViewModel.Theme = item.Tag.ToString()!;
                await ViewModel.SaveSettingsAsync();
            }
        }

        private async void StartupToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (_isLoading) return;
            ViewModel.StartWithWindows = StartupToggle.IsOn;
            await ViewModel.SaveSettingsAsync();
        }

        private async void TrayToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (_isLoading) return;
            ViewModel.TrayEnabled = TrayToggle.IsOn;
            await ViewModel.SaveSettingsAsync();
        }

        private async void AnimationsToggle_Toggled(object sender, RoutedEventArgs e)
        {
            if (_isLoading) return;
            ViewModel.AnimationsEnabled = AnimationsToggle.IsOn;
            await ViewModel.SaveSettingsAsync();
        }

        private void BackupButton_Click(object sender, RoutedEventArgs e)
        {
            ViewModel.ManualBackup();
        }

        private async void RestoreButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is StickerModel model)
            {
                await ViewModel.RestoreStickerAsync(model);
            }
        }

        private async void PurgeButton_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag is StickerModel model)
            {
                await ViewModel.PermanentlyDeleteStickerAsync(model);
            }
        }
    }
}
