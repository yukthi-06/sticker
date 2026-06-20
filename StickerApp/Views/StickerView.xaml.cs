using System;
using System.Diagnostics;
using System.IO;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.UI;
using Microsoft.UI.Windowing;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Input;
using Microsoft.UI.Xaml.Media;
using StickerApp.Services;
using StickerApp.ViewModels;
using Windows.Graphics;
using Windows.UI;

namespace StickerApp.Views
{
    public partial class StickerView : Window
    {
        public StickerViewModel ViewModel { get; }
        private readonly DesktopHostService _desktopHost;
        private readonly AnimationService _animation;

        private bool _isDragging;
        private PointInt32 _dragStartMousePosition;
        private PointInt32 _dragStartWindowPosition;

        private bool _isResizing;
        private PointInt32 _resizeStartMousePosition;
        private SizeInt32 _resizeStartWindowSize;

        public StickerView(StickerViewModel viewModel, DesktopHostService desktopHost, AnimationService animation)
        {
            this.InitializeComponent();
            ViewModel = viewModel;
            _desktopHost = desktopHost;
            _animation = animation;

            // Configure window properties before activation
            var appWindow = this.AppWindow;
            appWindow.TitleBar.ExtendsContentIntoTitleBar = true;
            appWindow.TitleBar.ButtonBackgroundColor = Colors.Transparent;
            appWindow.TitleBar.ButtonInactiveBackgroundColor = Colors.Transparent;
            appWindow.IsShownInSwitchers = false; // Hide from taskbar

            // Setup bindings/visual updates
            UpdateStickerVisuals();

            // Register events
            this.RootGrid.PointerEntered += RootGrid_PointerEntered;
            this.RootGrid.PointerExited += RootGrid_PointerExited;
            this.RootGrid.PointerMoved += RootGrid_PointerMoved;
            this.HeaderPanel.PointerReleased += Header_PointerReleased;
            
            ViewModel.LockedChanged += (s, locked) => UpdateLockVisuals(locked);
            ViewModel.AlwaysOnTopChanged += (s, top) => _desktopHost.SetAlwaysOnTop(ViewModel.Id, top);
            ViewModel.ClickThroughChanged += (s, ct) => _desktopHost.SetClickThrough(ViewModel.Id, ct);

            // Bind sizing changes
            this.SizeChanged += StickerView_SizeChanged;

            // Register and pin window
            _desktopHost.RegisterSticker(ViewModel.Id, this);

            if (ViewModel.IsAlwaysOnTop)
            {
                _desktopHost.SetAlwaysOnTop(ViewModel.Id, true);
            }
            else
            {
                _desktopHost.PinToDesktop(ViewModel.Id);
            }

            if (ViewModel.IsClickThrough)
            {
                _desktopHost.SetClickThrough(ViewModel.Id, true);
            }

            // Restore position
            appWindow.MoveAndResize(new RectInt32((int)ViewModel.X, (int)ViewModel.Y, (int)ViewModel.Width, (int)ViewModel.Height));

            _animation.ApplyFadeIn(this.RootGrid);
        }

        private void UpdateStickerVisuals()
        {
            TitleTextBlock.Text = ViewModel.Title;
            ContentTextBlock.Text = ViewModel.Text;
            ContentTextBlock.FontFamily = new FontFamily(ViewModel.Font);
            ContentTextBlock.FontSize = ViewModel.FontSize;

            // Opacity
            RootGrid.Opacity = ViewModel.Opacity;

            // Background color converting Hex to Color
            try
            {
                var hexColor = ViewModel.Color.TrimStart('#');
                byte r = byte.Parse(hexColor.Substring(0, 2), System.Globalization.NumberStyles.HexNumber);
                byte g = byte.Parse(hexColor.Substring(2, 2), System.Globalization.NumberStyles.HexNumber);
                byte b = byte.Parse(hexColor.Substring(4, 2), System.Globalization.NumberStyles.HexNumber);
                BackgroundBrush.Color = Color.FromArgb(255, r, g, b);
            }
            catch
            {
                BackgroundBrush.Color = Color.FromArgb(255, 255, 248, 176); // Fallback yellow
            }

            // Lock icon state
            UpdateLockVisuals(ViewModel.Locked);
        }

        private void UpdateLockVisuals(bool locked)
        {
            LockIcon.Glyph = locked ? "\uE72E" : "\uE785"; // Padlock vs unlocked padlock
        }

        private void RootGrid_PointerEntered(object sender, PointerRoutedEventArgs e)
        {
            if (!ViewModel.IsClickThrough)
            {
                _animation.ApplyFadeIn(HeaderPanel, 150);
            }
        }

        private void RootGrid_PointerExited(object sender, PointerRoutedEventArgs e)
        {
            _animation.ApplyFadeOut(HeaderPanel, () => HeaderPanel.Opacity = 0.0, 150);
        }

        // Custom window move via drag
        private void Header_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (ViewModel.Locked) return;

            var properties = e.GetCurrentPoint(HeaderPanel).Properties;
            if (properties.IsLeftButtonPressed)
            {
                _isDragging = true;
                HeaderPanel.CapturePointer(e.Pointer);
                
                // Get absolute mouse position via cursor
                var pos = GetCursorPosition();
                _dragStartMousePosition = pos;
                _dragStartWindowPosition = new PointInt32(AppWindow.Position.X, AppWindow.Position.Y);
            }
        }

        private void RootGrid_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragging)
            {
                var currentMouse = GetCursorPosition();
                int deltaX = currentMouse.X - _dragStartMousePosition.X;
                int deltaY = currentMouse.Y - _dragStartMousePosition.Y;

                var newX = _dragStartWindowPosition.X + deltaX;
                var newY = _dragStartWindowPosition.Y + deltaY;

                AppWindow.Move(new PointInt32(newX, newY));
                ViewModel.X = newX;
                ViewModel.Y = newY;
            }
        }

        private void Header_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_isDragging)
            {
                _isDragging = false;
                HeaderPanel.ReleasePointerCapture(e.Pointer);
            }
        }

        // Custom window resizer
        private void ResizeHandle_PointerPressed(object sender, PointerRoutedEventArgs e)
        {
            if (ViewModel.Locked) return;

            var canvas = (Canvas)sender;
            _isResizing = true;
            canvas.CapturePointer(e.Pointer);

            _resizeStartMousePosition = GetCursorPosition();
            _resizeStartWindowSize = AppWindow.Size;
        }

        private void ResizeHandle_PointerMoved(object sender, PointerRoutedEventArgs e)
        {
            if (_isResizing)
            {
                var currentMouse = GetCursorPosition();
                int deltaX = currentMouse.X - _resizeStartMousePosition.X;
                int deltaY = currentMouse.Y - _resizeStartMousePosition.Y;

                int newWidth = Math.Max(150, _resizeStartWindowSize.Width + deltaX);
                int newHeight = Math.Max(100, _resizeStartWindowSize.Height + deltaY);

                AppWindow.Resize(new SizeInt32(newWidth, newHeight));
                ViewModel.Width = newWidth;
                ViewModel.Height = newHeight;
            }
        }

        private void ResizeHandle_PointerReleased(object sender, PointerRoutedEventArgs e)
        {
            if (_isResizing)
            {
                _isResizing = false;
                ((Canvas)sender).ReleasePointerCapture(e.Pointer);
            }
        }

        private void StickerView_SizeChanged(object sender, WindowSizeChangedEventArgs args)
        {
            ViewModel.Width = AppWindow.Size.Width;
            ViewModel.Height = AppWindow.Size.Height;
        }

        private PointInt32 GetCursorPosition()
        {
            // PInvoke Helper to get cursor pos in screen space
            Interop.Win32.SetWindowPos(IntPtr.Zero, IntPtr.Zero, 0, 0, 0, 0, 0); // Trigger PInvoke mapping load
            return new PointInt32(CursorPositionHelper.GetX(), CursorPositionHelper.GetY());
        }

        // Action Handlers
        private void EditButton_Click(object sender, RoutedEventArgs e)
        {
            var editorDialog = new StickerEditorDialog(ViewModel);
            editorDialog.XamlRoot = this.Content.XamlRoot;
            _ = editorDialog.ShowAsync();
        }

        private void LockButton_Click(object sender, RoutedEventArgs e) => ViewModel.ToggleLock();
        private void ClickThroughButton_Click(object sender, RoutedEventArgs e) => ViewModel.ToggleClickThrough();
        private void DeleteButton_Click(object sender, RoutedEventArgs e) => _ = ViewModel.DeleteSticker();

        private void ContentTextBlock_DoubleTapped(object sender, DoubleTappedRoutedEventArgs e) => EditButton_Click(sender, e);
    }

    // Helper class to resolve cursor position via GDI PInvoke
    internal static class CursorPositionHelper
    {
        [System.Runtime.InteropServices.DllImport("user32.dll")]
        private static extern bool GetCursorPos(out POINT lpPoint);

        [System.Runtime.InteropServices.StructLayout(System.Runtime.InteropServices.LayoutKind.Sequential)]
        private struct POINT
        {
            public int X;
            public int Y;
        }

        public static int GetX()
        {
            GetCursorPos(out POINT p);
            return p.X;
        }

        public static int GetY()
        {
            GetCursorPos(out POINT p);
            return p.Y;
        }
    }
}
