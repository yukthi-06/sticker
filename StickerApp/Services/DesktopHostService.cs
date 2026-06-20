using System;
using System.Collections.Generic;
using StickerApp.Interop;
using Microsoft.UI.Xaml;
using WinRT.Interop;

namespace StickerApp.Services
{
    public class DesktopHostService
    {
        private readonly LoggerService _logger;
        private readonly Dictionary<Guid, IntPtr> _stickerWindows = new();
        private IntPtr _workerW = IntPtr.Zero;

        public DesktopHostService(LoggerService logger)
        {
            _logger = logger;
        }

        private IntPtr GetWorkerW()
        {
            if (_workerW == IntPtr.Zero)
            {
                _workerW = DesktopInterop.FindWallpaperWorkerW();
                _logger.LogInfo($"Resolved WorkerW handle: {_workerW}");
            }
            return _workerW;
        }

        public void RegisterSticker(Guid stickerId, Window window)
        {
            var hwnd = WindowNative.GetWindowHandle(window);
            _stickerWindows[stickerId] = hwnd;
            _logger.LogInfo($"Registered sticker {stickerId} with HWND {hwnd}");
        }

        public void UnregisterSticker(Guid stickerId)
        {
            if (_stickerWindows.Remove(stickerId))
            {
                _logger.LogInfo($"Unregistered sticker {stickerId}");
            }
        }

        public void PinToDesktop(Guid stickerId)
        {
            if (!_stickerWindows.TryGetValue(stickerId, out var hwnd)) return;

            var workerW = GetWorkerW();
            if (workerW != IntPtr.Zero)
            {
                // Set the window as a child of the desktop WorkerW
                Win32.SetParent(hwnd, workerW);

                // Modify window style to be a child window so it moves/minimizes with the desktop
                var style = Win32.GetWindowLongPtr(hwnd, Win32.GWL_STYLE);
                var newStyle = new IntPtr((style.ToInt64() & ~Win32.WS_POPUP) | Win32.WS_CHILD | Win32.WS_VISIBLE);
                Win32.SetWindowLongPtr(hwnd, Win32.GWL_STYLE, newStyle);

                // Update frame
                Win32.SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, 
                    Win32.SWP_NOMOVE | Win32.SWP_NOSIZE | Win32.SWP_NOACTIVATE | Win32.SWP_FRAMECHANGED);

                _logger.LogInfo($"Pinned sticker {stickerId} to WorkerW {workerW}");
            }
            else
            {
                _logger.LogWarning($"WorkerW not found, unable to pin sticker {stickerId} to desktop.");
            }
        }

        public void SetAlwaysOnTop(Guid stickerId, bool enabled)
        {
            if (!_stickerWindows.TryGetValue(stickerId, out var hwnd)) return;

            if (enabled)
            {
                // To be AlwaysOnTop, we must unparent it from WorkerW first
                Win32.SetParent(hwnd, IntPtr.Zero);

                var style = Win32.GetWindowLongPtr(hwnd, Win32.GWL_STYLE);
                var newStyle = new IntPtr((style.ToInt64() & ~Win32.WS_CHILD) | Win32.WS_POPUP | Win32.WS_VISIBLE);
                Win32.SetWindowLongPtr(hwnd, Win32.GWL_STYLE, newStyle);

                Win32.SetWindowPos(hwnd, Win32.HWND_TOPMOST, 0, 0, 0, 0, 
                    Win32.SWP_NOMOVE | Win32.SWP_NOSIZE | Win32.SWP_SHOWWINDOW | Win32.SWP_FRAMECHANGED);

                _logger.LogInfo($"Set sticker {stickerId} as Always-On-Top");
            }
            else
            {
                // Revert back to desktop pinned (WorkerW child)
                PinToDesktop(stickerId);
            }
        }

        public void SetClickThrough(Guid stickerId, bool enabled)
        {
            if (!_stickerWindows.TryGetValue(stickerId, out var hwnd)) return;

            var exStyle = Win32.GetWindowLongPtr(hwnd, Win32.GWL_EXSTYLE);
            IntPtr newExStyle;

            if (enabled)
            {
                newExStyle = new IntPtr(exStyle.ToInt64() | Win32.WS_EX_TRANSPARENT | Win32.WS_EX_LAYERED);
            }
            else
            {
                newExStyle = new IntPtr(exStyle.ToInt64() & ~(Win32.WS_EX_TRANSPARENT | Win32.WS_EX_LAYERED));
            }

            Win32.SetWindowLongPtr(hwnd, Win32.GWL_EXSTYLE, newExStyle);
            Win32.SetWindowPos(hwnd, IntPtr.Zero, 0, 0, 0, 0, 
                Win32.SWP_NOMOVE | Win32.SWP_NOSIZE | Win32.SWP_NOACTIVATE | Win32.SWP_FRAMECHANGED);

            _logger.LogInfo($"Set click-through for sticker {stickerId} to {enabled}");
        }

        public void SetNonActivated(Window window)
        {
            var hwnd = WindowNative.GetWindowHandle(window);
            var exStyle = Win32.GetWindowLongPtr(hwnd, Win32.GWL_EXSTYLE);
            var newExStyle = new IntPtr(exStyle.ToInt64() | Win32.WS_EX_NOACTIVATE | Win32.WS_EX_TOOLWINDOW);
            Win32.SetWindowLongPtr(hwnd, Win32.GWL_EXSTYLE, newExStyle);
        }
    }
}
