using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using StickerApp.ViewModels;

namespace StickerApp.Views
{
    public partial class StickerEditorDialog : ContentDialog
    {
        private readonly StickerViewModel _viewModel;

        public StickerEditorDialog(StickerViewModel viewModel)
        {
            this.InitializeComponent();
            _viewModel = viewModel;

            // Load initial configurations
            TitleInput.Text = _viewModel.Title;
            ContentInput.Text = _viewModel.Text;
            
            // Set font selection
            SelectFontComboBoxItem(_viewModel.Font);
            
            FontSizeInput.Value = _viewModel.FontSize;
            ColorHexInput.Text = _viewModel.Color;
            OpacityInput.Value = _viewModel.Opacity;
            AlwaysOnTopToggle.IsOn = _viewModel.IsAlwaysOnTop;
            ClickThroughToggle.IsOn = _viewModel.IsClickThrough;
        }

        private void SelectFontComboBoxItem(string fontName)
        {
            foreach (ComboBoxItem item in FontInput.Items)
            {
                if (item.Content?.ToString() == fontName)
                {
                    FontInput.SelectedItem = item;
                    return;
                }
            }
            FontInput.SelectedIndex = 0; // Default Segoe UI
        }

        private void ColorPreset_Click(object sender, RoutedEventArgs e)
        {
            if (sender is Button btn && btn.Tag != null)
            {
                ColorHexInput.Text = btn.Tag.ToString()!;
            }
        }

        private void ContentDialog_PrimaryButtonClick(ContentDialog sender, ContentDialogButtonClickEventArgs args)
        {
            // Apply fields back to VM
            _viewModel.Title = TitleInput.Text;
            _viewModel.Text = ContentInput.Text;
            
            if (FontInput.SelectedItem is ComboBoxItem fontItem)
            {
                _viewModel.Font = fontItem.Content?.ToString() ?? "Segoe UI";
            }
            
            _viewModel.FontSize = FontSizeInput.Value;
            _viewModel.Color = ColorHexInput.Text;
            _viewModel.Opacity = OpacityInput.Value;
            _viewModel.IsAlwaysOnTop = AlwaysOnTopToggle.IsOn;
            _viewModel.IsClickThrough = ClickThroughToggle.IsOn;
        }
    }
}
